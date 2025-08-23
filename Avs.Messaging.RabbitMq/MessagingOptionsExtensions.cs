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
            foreach (var consumerType in rabbitMqOptions.Consumers)
            {
                options.AddConsumer(consumerType);
            }
            
            services.AddSingleton<IMessageTransport, RabbitMqTransport>();

            if (rabbitMqOptions.UseRpcClient)
            {
                options.AddRpcClient(RabbitMqOptions.TransportName);
            }
        });
        
        return options;
    }
}