using Avs.Messaging.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avs.Messaging.Core;

public class MessageListenerHost(IMessageTransport? transport, ILogger<MessageListenerHost> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transport, "Unable to start MessageListenerHost: no transport configured");
        logger.LogInformation("Starting message listener host..");
        
        await transport!.InitAsync(cancellationToken);
        
        logger.LogInformation("Message listener host started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await transport!.DisposeAsync();
        logger.LogInformation("Message listener host stopped.");
    }
}