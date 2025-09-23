using Avs.Messaging.Core;

namespace Avs.Messaging.Contracts;

public interface IMessageTransport : IAsyncDisposable
{
    /// <summary>
    /// Transport type (In-memory, RabbitMq, ...)
    /// </summary>
    string TransportType { get; }
    
    /// <summary>
    /// Publishes a message
    /// </summary>
    /// <param name="message">Message to publish</param>
    /// <param name="publishOptions">Optional publish options</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <typeparam name="T">Type of message</typeparam>
    /// <returns>A task which resolves when a message is published.</returns>
    Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message
    /// </summary>
    /// <param name="message">Message to publish</param>
    /// <param name="messageType">Message type</param>
    /// <param name="publishOptions">Optional publish options</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task which resolves when a message is published.</returns>
    public Task PublishAsync(object message, Type messageType, PublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transport initialization logic
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task which resolves when a transport is ready to process message</returns>
    Task InitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Implements request-reply scenario
    /// </summary>
    /// <param name="request">Request message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TRequest">Type of request message</typeparam>
    /// <typeparam name="TResponse">Type of response</typeparam>
    /// <returns>A task which returns a response when resolves</returns>
    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}