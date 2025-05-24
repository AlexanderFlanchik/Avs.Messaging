using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.RabbitMq;

public static class MessagingOptionsExtensions
{
    public static MessagingOptions UseRabbitMq(this MessagingOptions options, Action<RabbitMqOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);
        
        var rabbitMqOptions = new RabbitMqOptions();
        configure(rabbitMqOptions);
        
        if (string.IsNullOrEmpty(rabbitMqOptions.Host))
        {
            throw new InvalidOperationException("RabbitMQ host URL is not configured");
        }
        
        options.ConfigureServices(services =>
        {
            services.AddSingleton(rabbitMqOptions);
            ServiceDescriptor? transportDescriptor = services.FirstOrDefault(s =>
                s.ServiceType == typeof(IMessageTransport));
            if (transportDescriptor is not null)
            {
                services.Remove(transportDescriptor);
            }
            
            services.AddSingleton<IMessageTransport, RabbitMqTransport>();
        });
        
        return options;
    }
}