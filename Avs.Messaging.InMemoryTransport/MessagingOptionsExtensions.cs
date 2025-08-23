using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.InMemoryTransport;

public static class MessagingOptionsExtensions
{
    public static MessagingOptions UseInMemoryTransport(this MessagingOptions options, Action<InMemoryTransportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var transportOptions = new InMemoryTransportOptions();
        options.ConfigureServices(services =>
        {
            configure?.Invoke(transportOptions);
            services.AddSingleton(transportOptions);
            services.AddSingleton<IMessageTransport, InMemoryTransport>();

            if (transportOptions.UseRpcClient)
            {
                options.AddRpcClient(InMemoryTransportOptions.TransportName);
            }
        });
        
        return options;
    }
}