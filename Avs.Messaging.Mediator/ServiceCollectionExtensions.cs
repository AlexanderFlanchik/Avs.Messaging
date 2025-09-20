using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.Mediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<MediatorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        
        services.AddScoped<IMediator, Mediator>();
        var mediatorOptions = new MediatorOptions(services);
        configure.Invoke(mediatorOptions);
        
        return services;
    }
}