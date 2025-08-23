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
        AddConsumer(consumerType);
    }

    /// <summary>
    /// Adds a message consumer. Consumers are registered as scoped services, receiving a new message creates a new scope.
    /// </summary>
    /// <param name="consumerType">Type of consumer</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddConsumer(Type consumerType)
    {
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
        
        services.AddScoped(consumerType);
    }

    /// <summary>
    /// Adds a filter for message consumer
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <typeparam name="TFilter">Type of filter implementation</typeparam>
    public void AddMessageFilter<TMessage, TFilter>() where TFilter: class, IMessageHandleFilter<TMessage>
    {
        services.AddScoped<IMessageHandleFilter<TMessage>, TFilter>();
    }
    
    public Dictionary<Type, List<Type>> ConsumerTypes => _consumerTypes;
    
    /// <summary>
    /// Adds a scoped client for request-reply messaging to DI container
    /// This client will use first default available transport from registered
    /// </summary>
    [Obsolete("Will be removed soon. Use AddRpcClient in transport specific options instead.")]
    public void AddRpcClient()
    {
        services.AddScoped<IRpcClient, RpcClient>();
    }

    /// <summary>
    /// Adds transport-specific client for request-reply messaging to DI container
    /// </summary>
    /// <param name="clientName">Client name</param>
    /// <param name="transportType">Name of transport to use</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddRpcClient(string transportType)
    {
        ArgumentException.ThrowIfNullOrEmpty(transportType);
        ArgumentException.ThrowIfNullOrEmpty(transportType);
        
        services.AddKeyedScoped<IRpcClient, RpcClient>(transportType, (sp, _) =>
        {
            var transport = sp.GetServices<IMessageTransport>().FirstOrDefault(t => t.TransportType == transportType);
            if (transport is null)
            {
                throw new InvalidOperationException($"The transport type {transportType} is not supported.");
            }
            
            return new RpcClient(transport, this);
        });
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