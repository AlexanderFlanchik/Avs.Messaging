using Avs.Messaging.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avs.Messaging.Core;

public class MessageListenerHost(IEnumerable<IMessageTransport> transports, ILogger<MessageListenerHost> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting message listener host..");
        
        await Task.WhenAll(transports.Select(t => t.InitAsync(cancellationToken)));
        
        logger.LogInformation("Message listener host started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(transports.Select(t => t.DisposeAsync().AsTask()));
        
        logger.LogInformation("Message listener host stopped.");
    }
}