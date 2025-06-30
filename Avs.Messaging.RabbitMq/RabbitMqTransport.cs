using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Avs.Messaging.RabbitMq;

internal class RabbitMqTransport( 
    RabbitMqOptions rabbitMqOptions, 
    IServiceProvider serviceProvider,
    ILogger<MessageListenerHost> logger) 
    : MessageTransportBase(serviceProvider), IMessageTransport
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IMessagePublisher? _publisher;
    private readonly ConcurrentDictionary<string, RequestReplyInfo> _requestReplyMap = new();
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default)
    {
       var consumerSettings = GetExchangeSettings(typeof(T));
       if (!string.IsNullOrEmpty(publishOptions?.CorrelationId))
       {
           consumerSettings.Props = consumerSettings.Props ?? new BasicProperties();
           consumerSettings.Props.CorrelationId = publishOptions.CorrelationId;
       }
       
       return PublishAsyncInternal(message!, consumerSettings, cancellationToken);
    }
    
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory() { HostName =  rabbitMqOptions.Host, Port = rabbitMqOptions.Port };
        _publisher = _serviceProvider.GetService<IMessagePublisher>();
        
        if (!string.IsNullOrEmpty(rabbitMqOptions.Username))
        {
            factory.UserName = rabbitMqOptions.Username;
        }

        if (!string.IsNullOrEmpty(rabbitMqOptions.Password))
        {
            factory.Password = rabbitMqOptions.Password;
        }
        
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        if (_connection is null)
        {
            throw new InvalidOperationException("Could not create connection");
        }
        
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var subscribers = GetSubscribers();
        
        int consumerCount = 0;
        foreach (var group in subscribers)
        {
            var consumers = group.ToList();
            foreach (var consumer in consumers)
            {
                await SubscribeAsync(group.Key, consumer, cancellationToken);
                consumerCount++;
            }
        }
        
        logger.LogInformation("Consumers found {consumerCount}", consumerCount);
    }

    public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tsc = new TaskCompletionSource<object?>();
        
        await using var ctr = cancellationToken.Register(() => tsc.TrySetCanceled());
        
        var queueName = $"{typeof(TRequest).FullName!}";
        var props = new BasicProperties()
        {
            CorrelationId = correlationId
        };

        var requestReplyInfo = new RequestReplyInfo(tsc, typeof(TResponse));
        _requestReplyMap.TryAdd(correlationId, requestReplyInfo);

        var requestConsumerSettings = new ExchangeOptions()
        {
            QueueName = queueName,
            ExchangeName = queueName,
            Props = props,
            RoutingKey = typeof(TRequest).FullName!,
            ExchangeType = ExchangeType.Direct
        };

        await PublishAsyncInternal(request!, requestConsumerSettings, cancellationToken);

        var response = (TResponse)(await tsc.Task)!;
        
        return response;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection is not null && _connection.IsOpen)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    private async Task PublishAsyncInternal(object message, ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default)
    {
        CheckConnectionAndChannel();

        if (!string.IsNullOrEmpty(exchangeOptions.ExchangeName))
        {
            try
            {
                await _channel!.ExchangeDeclareAsync(
                    exchange: exchangeOptions.ExchangeName!,
                    type: exchangeOptions.ExchangeType!,
                    durable: exchangeOptions.IsExchangeDurable,
                    autoDelete: !exchangeOptions.IsExchangeDurable,
                    cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
              logger.LogError(e, "Error during publishing message.");
              throw;
            }
        }

        var messageText = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(messageText);
        
        if (exchangeOptions.Props is not null)
        {
            await _channel!.BasicPublishAsync(
                exchangeOptions.ExchangeName ?? string.Empty, 
                exchangeOptions.RoutingKey ?? string.Empty,
                true,
                exchangeOptions.Props!,
                body,
                cancellationToken);
        }
        else
        {
            await _channel!.BasicPublishAsync(
                exchange: exchangeOptions.ExchangeName ?? string.Empty,
                routingKey: exchangeOptions.RoutingKey,
                body: body,
                cancellationToken: cancellationToken);
        }
    }
    
    private async Task SubscribeAsync(Type messageType, IConsumer consumer, CancellationToken cancellationToken = default)
    {
        var consumerSettings = GetExchangeSettings(messageType);
        
        var isExchangeDurable = consumerSettings.IsExchangeDurable;
        var isQueueDurable = consumerSettings.IsQueueDurable;
        var isExclusive = consumerSettings.IsQueueExclusive;
        var exchangeName = consumerSettings.ExchangeName ?? messageType.FullName!;
        var exchangeType = consumerSettings.ExchangeType;
        
        await _channel!.ExchangeDeclareAsync(exchangeName, exchangeType, isExchangeDurable, !isExchangeDurable, 
            cancellationToken: cancellationToken);

        var queueName = !consumerSettings.IsRequestReply ? GetQueueName(consumerSettings) : messageType.FullName!;
        if (consumerSettings.IsRequestReply && !string.IsNullOrEmpty(consumerSettings.RequestType))
        {
            await SubscribeToRpcErrorsAsync(consumerSettings.RequestType, cancellationToken);
        }

        await _channel!.QueueDeclareAsync(queueName, isQueueDurable, isExclusive, !isQueueDurable,
            cancellationToken: cancellationToken);
        
        await _channel!.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: consumerSettings?.RoutingKey ?? string.Empty,
            cancellationToken: cancellationToken
        );
        
        var channelConsumer = new AsyncEventingBasicConsumer(_channel!);
        channelConsumer.ReceivedAsync += async (_, ea) => 
            await ReceiveMessageAsync(ea, messageType, consumer, consumerSettings!.IsRequestReply);
        
        await _channel!.BasicConsumeAsync(queueName, autoAck: false, consumer: channelConsumer, cancellationToken);
    }

    private async Task SubscribeToRpcErrorsAsync(string requestType, CancellationToken cancellationToken = default)
    {
        var errorExchange = $"{requestType}.{rabbitMqOptions.RequestReplyErrorQueue}";
        await _channel!.ExchangeDeclareAsync(errorExchange, ExchangeType.Direct, false, true, 
            cancellationToken: cancellationToken);
        
        await _channel!.QueueDeclareAsync(errorExchange, false, false, true,
            cancellationToken: cancellationToken);
        
        await _channel!.QueueBindAsync(
            queue: errorExchange,
            exchange: errorExchange,
            routingKey: errorExchange,
            cancellationToken: cancellationToken
        );
        
        var channelConsumer = new AsyncEventingBasicConsumer(_channel!);
        channelConsumer.ReceivedAsync += async (_, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId ?? string.Empty;
            _requestReplyMap.TryGetValue(correlationId, out RequestReplyInfo? requestInfo);
            if (requestInfo is null)
            {
                return;
            }
            
            var error = Encoding.UTF8.GetString(ea.Body.Span.ToArray());
            var exception = new RequestReplyException(RequestReplyError.HandlerError, error);
            requestInfo.TaskCompletionSource?.TrySetException(exception);
            
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ea.CancellationToken);
            _requestReplyMap.Remove(correlationId, out var _);
        };
        
        await _channel!.BasicConsumeAsync(errorExchange, autoAck: false, consumer: channelConsumer, cancellationToken);
    }

    private async Task ReceiveMessageAsync(BasicDeliverEventArgs ea, Type messageType, IConsumer consumer, bool isRequestReply = false)
    {
        var correlationId = ea.BasicProperties.CorrelationId ?? string.Empty;
        _requestReplyMap.TryGetValue(correlationId, out RequestReplyInfo? requestInfo);
            
        try
        {
            var message = JsonSerializer.Deserialize(ea.Body.Span, messageType)!;
            if (requestInfo is not null)
            {
                var tcs = requestInfo.TaskCompletionSource!;
                tcs.TrySetResult(message);
                _requestReplyMap.Remove(correlationId, out _);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, ea.CancellationToken);
                return;
            }
                
            var context = new ConsumerContext()
            {
                Message = message,
                Headers = ea.BasicProperties.Headers,
                CorrelationId = ea.BasicProperties.CorrelationId,
                MessagePublisher = _publisher!,
            };
                
            await consumer.Consume(context);
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ea.CancellationToken);
        }
        catch (Exception e)
        {
            requestInfo?.TaskCompletionSource?.TrySetException(e);

            if (!string.IsNullOrEmpty(correlationId) && isRequestReply)
            {
                var errorExchange = $"{messageType.FullName}.{rabbitMqOptions.RequestReplyErrorQueue}";
                var body = Encoding.UTF8.GetBytes(e.Message);
                
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, ea.CancellationToken);
                
                await _channel!.BasicPublishAsync(
                    errorExchange,
                    errorExchange,
                    true,
                    new BasicProperties() { CorrelationId = correlationId },
                    body,
                    ea.CancellationToken);
            }
            else
            {
                await _channel!.BasicRejectAsync(ea.DeliveryTag, true, ea.CancellationToken);
            }

            logger.LogError(e, "Message processing failed.");
        }
    }
    
    private ExchangeOptions GetExchangeSettings(Type messageType)
    {
        rabbitMqOptions.ExchangeSettings.TryGetValue(messageType, out var consumerSettings);
        return consumerSettings ?? 
               new ExchangeOptions()
               {
                   ExchangeName = messageType.FullName!,
                   ExchangeType = ExchangeType.Fanout, 
                   RoutingKey = string.Empty
               };
    }

    private string GetQueueName(ExchangeOptions? consumerSettings)
    {
        var queueNameTail = consumerSettings?.IsQueueDurable == true ? rabbitMqOptions.ServiceId : Guid.NewGuid().ToString("N");
        
        return $"{consumerSettings?.QueueName}_{queueNameTail}";
    }

    private void CheckConnectionAndChannel()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("RabbitMQ connection has not been initialized");
        }

        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel has not been initialized");
        }
    }
}