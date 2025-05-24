using Avs.Messaging.Contracts;

namespace Avs.Messaging.Core;

public sealed class MessageContext<T>
{
    /// <summary>
    /// Message correlation ID
    /// </summary>
    public string? CorrelationId { get; private init; }

    /// <summary>
    /// Message headers
    /// </summary>
    public IDictionary<string, object?>? Headers { get; private init; }
    
    // Strong typed message body
    public T Message { get; private set; } = default!;
    
    // Publish endpoint
    public IMessagePublisher MessagePublisher { get; private init; } = default!;
    
    internal static MessageContext<T> Create(
        T message, 
        ConsumerContext context) 
        => new()
        {
            Message = message,
            Headers = context.Headers,
            CorrelationId = context.CorrelationId,
            MessagePublisher = context.MessagePublisher
        };
}