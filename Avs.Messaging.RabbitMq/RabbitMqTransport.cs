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
    MessagingOptions messagingOptions,
    IServiceProvider serviceProvider,
    ILogger<MessageListenerHost> logger) 
    : IMessageTransport
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IMessagePublisher? _publisher;
    private readonly ConcurrentDictionary<string, RequestReplyInfo> _requestReplyMap = new();

    public string TransportType => RabbitMqOptions.TransportName;
    
    public Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default)
    { 
        return PublishAsync(message!, typeof(T), publishOptions, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, PublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
    {
        var consumerSettings = GetExchangeSettings(messageType);
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
        _publisher = serviceProvider.GetService<IMessagePublisher>();
        
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
        
        int consumerCount = 0;
        foreach (var group in  messagingOptions.ConsumerTypes)
        {
            var consumers = group.Value;
            foreach (var consumer in consumers)
            {
                if (!ShouldSubscribe(consumer))
                {
                    continue;
                }

                try
                {
                    await SubscribeAsync(group.Key, consumer, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error subscribing consumer {consumer}", consumer);
                    continue;
                }

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
        
        var props = new BasicProperties()
        {
            CorrelationId = correlationId
        };

        var requestReplyInfo = new RequestReplyInfo(tsc, typeof(TResponse));
        _requestReplyMap.TryAdd(correlationId, requestReplyInfo);

        var requestExchangeSettings = GetExchangeSettings(typeof(TRequest));
        requestExchangeSettings.Props = props;
        requestExchangeSettings.ExchangeType = ExchangeType.Direct;
        requestExchangeSettings.IsQueueDurable = true;
        requestExchangeSettings.IsExchangeDurable = true;

        if (string.IsNullOrEmpty(requestExchangeSettings.ExchangeName))
        {
            requestExchangeSettings.ExchangeName = typeof(TRequest).FullName!;
        }

        if (string.IsNullOrEmpty(requestExchangeSettings.RoutingKey))
        {
            requestExchangeSettings.RoutingKey = typeof(TRequest).FullName!;
        }

        await PublishAsyncInternal(request!, requestExchangeSettings, cancellationToken);

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

    private bool ShouldSubscribe(Type consumerType)
    {
        return rabbitMqOptions.Consumers.Count == 0 ||
               rabbitMqOptions.Consumers.Contains(consumerType);
    }

    private async Task PublishAsyncInternal(object message, ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(cancellationToken);

        if (!string.IsNullOrEmpty(exchangeOptions.ExchangeName))
        {
            try
            {
                await EnsureExchangeDeclaredAsync(
                    exchangeName: exchangeOptions.ExchangeName!,
                    exchangeType: exchangeOptions.ExchangeType!,
                    durable: exchangeOptions.IsExchangeDurable,
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
    
    private async Task SubscribeAsync(Type messageType, Type consumerType, CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(cancellationToken);

        var consumerSettings = GetExchangeSettings(messageType);
        
        var isExchangeDurable = consumerSettings.IsExchangeDurable;
        var isQueueDurable = consumerSettings.IsQueueDurable;
        var isExclusive = consumerSettings.IsQueueExclusive;
        var exchangeName = consumerSettings.ExchangeName ?? messageType.FullName!;
        var exchangeType = consumerSettings.ExchangeType;
        
        await EnsureExchangeDeclaredAsync(exchangeName, exchangeType, isExchangeDurable, cancellationToken);

        var queueName = !consumerSettings.IsRequestReply ? GetQueueName(consumerSettings) : messageType.FullName!;
        if (consumerSettings.IsRequestReply && !string.IsNullOrEmpty(consumerSettings.RequestType))
        {
            await SubscribeToRpcErrorsAsync(consumerSettings.RequestType, cancellationToken);
        }

        await EnsureQueueDeclaredAsync(queueName, isQueueDurable, isExclusive, cancellationToken);
        
        await _channel!.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            arguments: new Dictionary<string, object?>()
            {
                ["x-expires"] = 86400000
            },
            routingKey: consumerSettings?.RoutingKey ?? string.Empty,
            cancellationToken: cancellationToken
        );
        
        var channelConsumer = new AsyncEventingBasicConsumer(_channel!);
        channelConsumer.ReceivedAsync += async (_, ea) =>
        {
            using var scope = serviceProvider.CreateScope();
            if (scope.ServiceProvider.GetService(consumerType) is not IConsumer consumer)
            {
                return;
            }
            
            var filterType = typeof(IMessageHandleFilter<>).MakeGenericType(messageType);
            var filters = scope.ServiceProvider.GetServices(filterType);
            
            await ReceiveMessageAsync(ea, messageType, consumer, filters, consumerSettings!.IsRequestReply);
        };

        await _channel!.BasicConsumeAsync(queueName, autoAck: false, consumer: channelConsumer, cancellationToken);
    }

    private async Task SubscribeToRpcErrorsAsync(string requestType, CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(cancellationToken);

        var errorExchange = $"{requestType}.{rabbitMqOptions.RequestReplyErrorQueue}";
        await EnsureExchangeDeclaredAsync(errorExchange, ExchangeType.Direct, true, cancellationToken);
        
        await EnsureQueueDeclaredAsync(errorExchange, true, false, cancellationToken);
        
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

    private async Task ReceiveMessageAsync(BasicDeliverEventArgs ea, Type messageType, IConsumer consumer, IEnumerable<object?> filters, bool isRequestReply = false)
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
                Filters = filters
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
    
    private async Task EnsureExchangeDeclaredAsync(string exchangeName, string exchangeType, bool durable, CancellationToken cancellationToken)
    {
        var channel = await EnsureChannelAsync(cancellationToken);

        try
        {
            await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable, !durable, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (IsTopologyConflict(ex))
        {
            logger.LogWarning(ex, "Exchange {ExchangeName} already exists with incompatible topology; continuing with the existing exchange.", exchangeName);
            _channel = await CreateChannelAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is global::RabbitMQ.Client.Exceptions.AlreadyClosedException || ex is global::RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            _channel = await CreateChannelAsync(cancellationToken);
            try
            {
                await _channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable, !durable, cancellationToken: cancellationToken);
            }
            catch (Exception innerEx) when (IsTopologyConflict(innerEx))
            {
                logger.LogWarning(innerEx, "Exchange {ExchangeName} already exists with incompatible topology; continuing with the existing exchange.", exchangeName);
            }
        }
    }

    private async Task EnsureQueueDeclaredAsync(string queueName, bool durable, bool exclusive, CancellationToken cancellationToken)
    {
        var channel = await EnsureChannelAsync(cancellationToken);

        try
        {
            await channel.QueueDeclareAsync(queueName, durable, exclusive, !durable, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (IsTopologyConflict(ex))
        {
            logger.LogWarning(ex, "Queue {QueueName} already exists with incompatible topology; continuing with the existing queue.", queueName);
            _channel = await CreateChannelAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is global::RabbitMQ.Client.Exceptions.AlreadyClosedException || ex is global::RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            _channel = await CreateChannelAsync(cancellationToken);
            try
            {
                await _channel.QueueDeclareAsync(queueName, durable, exclusive, !durable, cancellationToken: cancellationToken);
            }
            catch (Exception innerEx) when (IsTopologyConflict(innerEx))
            {
                logger.LogWarning(innerEx, "Queue {QueueName} already exists with incompatible topology; continuing with the existing queue.", queueName);
            }
        }
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        return await CreateChannelAsync(cancellationToken);
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("RabbitMQ connection has not been initialized");
        }

        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        return _channel;
    }

    private static bool IsTopologyConflict(Exception ex)
    {
        return ex is global::RabbitMQ.Client.Exceptions.OperationInterruptedException { ShutdownReason: not null } operationInterrupted
            && (operationInterrupted.ShutdownReason.ReplyCode == 406
                || operationInterrupted.ShutdownReason.ReplyText.Contains("PRECONDITION_FAILED", StringComparison.OrdinalIgnoreCase)
                || operationInterrupted.ShutdownReason.ReplyText.Contains("inequivalent arg", StringComparison.OrdinalIgnoreCase))
            || ex is global::RabbitMQ.Client.Exceptions.AlreadyClosedException { ShutdownReason: not null } alreadyClosed
            && (alreadyClosed.ShutdownReason.ReplyCode == 406
                || alreadyClosed.ShutdownReason.ReplyText.Contains("PRECONDITION_FAILED", StringComparison.OrdinalIgnoreCase)
                || alreadyClosed.ShutdownReason.ReplyText.Contains("inequivalent arg", StringComparison.OrdinalIgnoreCase))
            || ex.Message.Contains("PRECONDITION_FAILED", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("inequivalent arg", StringComparison.OrdinalIgnoreCase);
    }

    private ExchangeOptions GetExchangeSettings(Type messageType)
    {
        rabbitMqOptions.ExchangeSettings.TryGetValue(messageType, out var consumerSettings);
        return consumerSettings ?? 
               new ExchangeOptions()
               {
                   ExchangeName = messageType.FullName!,
                   ExchangeType = ExchangeType.Fanout,
                   IsQueueDurable = true,
                   IsExchangeDurable = true,
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