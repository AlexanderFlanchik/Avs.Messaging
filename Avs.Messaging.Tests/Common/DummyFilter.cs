using Avs.Messaging.Core;

namespace Avs.Messaging.Tests.Common;

public class DummyFilter(IFilterVerifier filterVerifier) : IMessageHandleFilter<Greeting>
{
    public async Task HandleAsync(MessageContext<Greeting> context, MessageHandlerDelegate<Greeting> next)
    {
        filterVerifier.VerifyBeforeAction(context.Message.Message);
        context.Message.Message = "HELLO";
            
        await next(context);
            
        filterVerifier.VerifyAfterAction(context.Message.Message);
    }
}