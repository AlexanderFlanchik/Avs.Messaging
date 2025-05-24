using Avs.Messaging.Core;

namespace Avs.Messaging.Contracts;

public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message of type <see cref="T"/>
    /// </summary>
    /// <param name="message">A message object to publish</param>
    /// <param name="publishOptions">A publish options object with correlation ID and headers (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="T">A message type</typeparam>
    /// <returns>An async operation which resolves when a message is published.</returns>
    Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default);
}