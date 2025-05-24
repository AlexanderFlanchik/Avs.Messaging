using Avs.Messaging.Contracts;
using Avs.Messaging.Core;

namespace Avs.Messaging.Tests.Common;

public class GreetingConsumer(IMessageVerifier messageVerifier) : ConsumerBase<Greeting>
{
    protected override Task Consume(MessageContext<Greeting> messageContext)
    {
        messageVerifier.SetMessage(messageContext.Message);
        
        return Task.CompletedTask;
    }
}