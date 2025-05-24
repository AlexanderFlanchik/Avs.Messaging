using System.Collections.Concurrent;
using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avs.Messaging.InMemoryTransport;

internal class InMemoryTransport :  MessageTransportBase, IMessageTransport
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryTransport> _logger;
    private readonly ILookup<Type, IConsumer> _consumerLookup;
    private readonly ConcurrentDictionary<string, RequestReplyInfo> _requestsMap = new();
    private IMessagePublisher? _publisher;

    public InMemoryTransport(IServiceProvider serviceProvider, ILogger<InMemoryTransport> logger) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _consumerLookup = GetSubscribers();
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public async Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default)
    {
        var consumers = _consumerLookup.Where(k => k.Key == typeof(T))
            .SelectMany(v => v.ToList()).ToArray();
        
        if (consumers.Length == 0)
        {
            _logger.LogWarning("No subscribers found for type {MessageType}", typeof(T));
            return;
        }

        var correlationId = publishOptions?.CorrelationId;
        var isRequestReply = publishOptions?.IsRequestReply ?? false;
        
        if (correlationId is not null && isRequestReply && _requestsMap.TryGetValue(correlationId, out var replyInfo))
        {
            replyInfo.TaskCompletionSource.SetResult(message);
            _requestsMap.TryRemove(correlationId, out _);
            return;
        }

        var context = new ConsumerContext()
        {
            Message = message!,
            Headers = publishOptions?.Headers,
            CorrelationId = correlationId,
            MessagePublisher = _publisher!
        };

        async Task SafeConsumeAsync(IConsumer consumer, ConsumerContext consumerContext)
        {
            try
            {
                await consumer.Consume(consumerContext);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occured while consuming a message.");
            }
        }

        await Parallel.ForEachAsync(consumers, cancellationToken, async (consumer, _) => await SafeConsumeAsync(consumer, context));
    }

    public Task InitAsync(CancellationToken cancellationToken = default)
    {
        _publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
        return Task.CompletedTask;
    }

    public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        var requestConsumer = _consumerLookup.FirstOrDefault(k => k.Key == typeof(TRequest));
        if (requestConsumer is null)
        {
            throw new RequestReplyException(RequestReplyError.HandlerError, 
                    $"No request consumer registered for type {typeof(TRequest)}.");
        }
        
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _requestsMap.TryAdd(correlationId, new RequestReplyInfo(tcs, typeof(TResponse)));
        
        await PublishAsync(request, new PublishOptions() { CorrelationId = correlationId }, cancellationToken);
        
        var response = await tcs.Task;

        return (TResponse)response!;
    }
}