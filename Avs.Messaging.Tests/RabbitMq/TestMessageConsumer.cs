using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.Tests.Common;

namespace Avs.Messaging.Tests.RabbitMq;

public class TestMessageConsumer(IMessageVerifier messageVerifier) : ConsumerBase<TestMessage>
{
    protected override Task Consume(MessageContext<TestMessage> messageContext)
    {
        messageVerifier.SetMessage(messageContext.Message);
        return Task.CompletedTask;
    }
}