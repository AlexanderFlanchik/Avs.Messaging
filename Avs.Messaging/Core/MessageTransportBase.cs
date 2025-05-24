using Avs.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.Core;

public abstract class MessageTransportBase(IServiceProvider serviceProvider)
{
    protected ILookup<Type, IConsumer> GetSubscribers()
    {
        var subscribers = serviceProvider.GetServices<IConsumer>()
            .Where(c => c.GetType().BaseType?.Name == typeof(ConsumerBase<>).Name)
            .ToLookup(c => c.GetType().BaseType!.GenericTypeArguments[0]); 
      
        return subscribers;
    }
}