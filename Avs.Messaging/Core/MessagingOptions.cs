using Avs.Messaging.Contracts;
using Avs.Messaging.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Avs.Messaging.Core;

public class MessagingOptions(IServiceCollection services)
{
    private readonly Dictionary<Type, List<Type>> _consumerTypes = new(); // key - message type, value - consumer types
    
    public TimeSpan RequestReplyTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Adds a message consumer. Consumers are registered as scoped services, receiving a new message creates a new scope.
    /// </summary>
    /// <typeparam name="T">Type of consumer</typeparam>
    public void AddConsumer<T>() where T : class, IConsumer
    {
        var consumerType = typeof(T);
        var baseType = consumerType.BaseType;
       
        if (baseType is null || baseType.GetGenericTypeDefinition() != typeof(ConsumerBase<>))
        {
            throw new InvalidOperationException($"The consumer type {consumerType.FullName} is not a inherited from ConsumerBase type.");
        }
        
        var messageType = baseType.GenericTypeArguments.FirstOrDefault()!;
        if (_consumerTypes.TryGetValue(messageType, out var lst) && !lst.Contains(consumerType))
        {
            lst.Add(consumerType);
        }
        else
        {
            lst = [consumerType];
            _consumerTypes.Add(messageType, lst);
        }
        
        services.AddScoped<T>();
    }
    
    public Dictionary<Type, List<Type>> ConsumerTypes => _consumerTypes;
    
    /// <summary>
    /// Sets <see cref="IMessageTransport"/> as message transport
    /// </summary>
    /// <param name="transport">Actual transport instance, registered as singleton.</param>
    public void SetTransport(IMessageTransport transport)
    {
        services.RemoveAll(typeof(IMessageTransport));
        services.AddSingleton(transport);
    }

    /// <summary>
    /// Adds a scoped client for request-reply messaging to DI container
    /// </summary>
    public void AddRpcClient()
    {
        services.AddScoped<IRpcClient, RpcClient>();
    }

    /// <summary>
    /// Adds additional options/services to DI container
    /// </summary>
    /// <param name="configure">Delegate which configures <see cref="IServiceCollection"/></param>
    public void ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(services);
    }
}