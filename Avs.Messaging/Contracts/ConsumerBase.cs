using Avs.Messaging.Core;

namespace Avs.Messaging.Contracts;

public abstract class ConsumerBase<T> : IConsumer
{
    /// <summary>
    /// A message handler
    /// </summary>
    /// <param name="messageContext">Context with message payload, headers and correlation ID</param>
    protected abstract Task Consume(MessageContext<T> messageContext);
    
    /// <summary>
    /// Consumes a message
    /// </summary>
    /// <param name="context">Consumer context with message payload, headers, and correlation ID</param>
    public async Task Consume(ConsumerContext context)
    {
        if (context.Message is not T payload)
        {
            return;
        }

        var messageContext = MessageContext<T>.Create(payload, context);
        
        await Consume(messageContext);
    }

    /// <summary>
    /// Responds to request in request-reply flow
    /// </summary>
    /// <param name="response">Response sent to response queue</param>
    /// <param name="requestContext">Request message context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <typeparam name="TRequest">Request type</typeparam>
    protected async Task RespondAsync<TResponse, TRequest>(TResponse response, MessageContext<TRequest> requestContext, 
        CancellationToken cancellationToken = default)
    {
        var publishOptions = new PublishOptions()
        {
            Headers = requestContext.Headers, 
            CorrelationId = requestContext.CorrelationId,
            IsRequestReply = true
        };

        await requestContext.MessagePublisher.PublishAsync(response, publishOptions, cancellationToken);
    }
}