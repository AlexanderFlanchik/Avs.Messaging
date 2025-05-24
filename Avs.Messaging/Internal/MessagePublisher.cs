using Avs.Messaging.Contracts;
using Avs.Messaging.Core;

namespace Avs.Messaging.Internal;

internal class MessagePublisher(IMessageTransport transport) : IMessagePublisher
{
    public Task PublishAsync<T>(T message, PublishOptions? publishOptions = null, CancellationToken cancellationToken = default) 
        => transport.PublishAsync(message, publishOptions, cancellationToken);
}