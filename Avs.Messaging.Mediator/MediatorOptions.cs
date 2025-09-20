using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.Mediator;

public class MediatorOptions(IServiceCollection services)
{
    public MediatorOptions AddRequestHandler<TRequest, THandler>()
    {
        var type = typeof(THandler);
        var  requestHandlerType = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && (
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))
            );
        
        if (requestHandlerType is null)
        {
            throw new InvalidOperationException($"Type '{typeof(THandler).FullName}' is not a request handler");
        }
        
        Type[] genArgs = requestHandlerType.GetGenericArguments();
        Type handlerType;
        if (genArgs.Length > 1)
        {
            Type responseType = requestHandlerType.GetGenericArguments()[1];
            handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TRequest), responseType);
        }
        else
        {
            handlerType = typeof(IRequestHandler<>).MakeGenericType(typeof(TRequest));   
        }
        
        services.AddTransient(handlerType, type);

        return this;
    }

    public MediatorOptions AddNotificationHandler<THandler>()
    {
        var type = typeof(THandler);
        var notificationHandlerType = type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(INotificationHandler<>));
        if (notificationHandlerType is null)
        {
            throw new InvalidOperationException($"Type '{typeof(THandler).FullName}' is not a notification handler");
        }
        
        var notificationType = notificationHandlerType.GetGenericArguments()[0];
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        services.AddTransient(handlerType, type);
        
        return this;
    }
}