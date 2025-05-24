using Avs.Messaging.Contracts;
using Avs.Messaging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Avs.Messaging.Core;

public class MessagingOptions
{
    private readonly IServiceCollection _services;
    
    public TimeSpan RequestReplyTimeout { get; set; } = TimeSpan.FromSeconds(60);

    public MessagingOptions(IServiceCollection services)
    {
        _services = services;
    }
    
    /// <summary>
    /// Adds a message consumer. Consumers are registered as singletons.
    /// </summary>
    /// <typeparam name="T">Type of consumer</typeparam>
    public void AddConsumer<T>() where T : class, IConsumer
    {
        _services.AddSingleton<IConsumer, T>();
    }

    /// <summary>
    /// Sets <see cref="IMessageTransport"/> as message transport
    /// </summary>
    /// <param name="transport">Actual transport instance, registered as singleton.</param>
    public void SetTransport(IMessageTransport transport)
    {
        _services.RemoveAll(typeof(IMessageTransport));
        _services.AddSingleton(transport);
    }

    /// <summary>
    /// Adds a scoped client for request-reply messaging to DI container
    /// </summary>
    public void AddRpcClient()
    {
        _services.AddScoped<IRpcClient, RpcClient>();
    }

    /// <summary>
    /// Adds additional options/services to DI container
    /// </summary>
    /// <param name="configure">Delegate which configures <see cref="IServiceCollection"/></param>
    public void ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
    }
}