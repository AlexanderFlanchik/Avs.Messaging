using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.InMemoryTransport;

public static class MessagingOptionsExtensions
{
    public static MessagingOptions UseInMemoryTransport(this MessagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ConfigureServices(services =>
        {
            ServiceDescriptor? transportDescriptor = services.FirstOrDefault(s =>
                s.ServiceType == typeof(IMessageTransport));
            
            if (transportDescriptor is not null)
            {
                services.Remove(transportDescriptor);
            }

            services.AddSingleton<IMessageTransport, InMemoryTransport>();
        });
        
        return options;
    }
}