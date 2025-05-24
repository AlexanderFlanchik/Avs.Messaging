using Avs.Messaging.Contracts;
using Avs.Messaging.Core;

namespace Avs.Messaging.Tests.Common;

public class PingConsumer : ConsumerBase<Ping>
{
    protected override async Task Consume(MessageContext<Ping> messageContext)
    {
        var ping = messageContext.Message;
        await Task.Delay(200);
        
        var pong = new Pong(ping.Id, DateTime.UtcNow);

        await RespondAsync(pong, messageContext);
    }
}