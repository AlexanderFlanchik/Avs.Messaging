using Avs.Messaging.Contracts;
using Avs.Messaging.Core;

namespace Avs.Messaging.Internal;

internal class MessagePublisher(IEnumerable<IMessageTransport> transports) : IMessagePublisher
{
    public Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default)
        => PublishAsync(message!, typeof(T), publishOptions, cancellationToken);

    public Task PublishAsync(object message, Type messageType, PublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
        => Task.WhenAll(
            transports.Select(transport => transport.PublishAsync(message, messageType, publishOptions, cancellationToken))
        );
}