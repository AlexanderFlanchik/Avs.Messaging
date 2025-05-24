using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services,
        Action<MessagingOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configureOptions, nameof(configureOptions));
        
        services.AddSingleton<MessageListenerHost>();
        services.AddTransient<IMessagePublisher, MessagePublisher>();
        services.AddHostedService(sp => sp.GetRequiredService<MessageListenerHost>());
        
        var messagingOptions = new MessagingOptions(services);
        services.AddSingleton(messagingOptions);
        configureOptions(messagingOptions);
        
        return services;
    }
}