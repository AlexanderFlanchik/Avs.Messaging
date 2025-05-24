using Avs.Messaging.Contracts;
using Avs.Messaging.Core;

namespace Avs.Messaging.Tests.Common;

public class PongConsumer : ConsumerBase<Pong>
{
    protected override Task Consume(MessageContext<Pong> messageContext)
    {
        // Nothing to do here
        return Task.CompletedTask;
    }
}