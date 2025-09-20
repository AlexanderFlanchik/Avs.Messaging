namespace Avs.Messaging.Mediator;

public interface INotificationHandler<in T> where T : INotification
{
    Task HandleAsync(T notification, CancellationToken cancellationToken = default);
}

public enum NotificationMode
{
    Parallel,
    Concurrent
}