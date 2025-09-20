using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.Mediator;

public interface IMediator
{
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;

    Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    Task PublishAsync<TNotification>(TNotification notification, NotificationMode mode = NotificationMode.Parallel, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
        
        return handler is not null ? 
            await handler.HandleAsync(request, cancellationToken) : 
            throw new InvalidOperationException($"No handler of type {typeof(IRequestHandler<TRequest, TResponse>).FullName} found.");
    }

    public async Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
        if (handler is null)
        {
            throw new InvalidOperationException($"No handler of type {typeof(IRequestHandler<TRequest>).FullName} found.");
        }
        
        await handler.HandleAsync(request, cancellationToken);
    }

    public async Task PublishAsync<TNotification>(TNotification notification, NotificationMode mode = NotificationMode.Parallel, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>()
            .ToArray();
        
        if (handlers.Length == 0)
        {
            return;
        }

        if (mode == NotificationMode.Concurrent)
        {
            foreach (var handler in handlers)
            {
               await NotifySafe(handler);
            }
        }
        else
        {
            await Task.WhenAll(handlers.Select(NotifySafe));
        }

        return;

        async Task NotifySafe(INotificationHandler<TNotification> handler)
        {
            try
            {
                await handler.HandleAsync(notification, cancellationToken);
            }
            catch
            {
                // no-op   
            }
        }
    }
}