using Avs.Messaging.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Avs.Messaging.Tests.Mediator;

public class MediatorTests
{
    [Test]
    public async Task SendAsync_ShouldSendRequestToSpecificHandler_WithResponse()
    {
        // Arrange
        var mediator = GetMediator(
            _ => { }, 
            cfg => cfg.AddRequestHandler<TestMessage, MessageHandler>());
        
        // Act
        var response = await mediator.SendAsync<TestMessage, TestResponse>(new TestMessage("test"));
        
        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Value, Is.EqualTo("TEST"));
    }

    [Test]
    public async Task SendAsync_ShouldSendRequestToSpecificHandler_WithoutResponse()
    {
        // Arrange
        var receiverMock = new Mock<IReceiver>();
        
        var mediator = GetMediator(
            services => services.AddSingleton(sp => receiverMock.Object),
            cfg => cfg.AddRequestHandler<TestMessageWithoutResponse, MessageWithoutResponseHandler>());
        
        // Act
        await mediator.SendAsync<TestMessageWithoutResponse>(new TestMessageWithoutResponse("TEST"));
        
        // Assert
        receiverMock.Verify(x => x.Receive("TEST"), Times.Once);
    }
    
    [Test]
    public async Task PublishAsync_ShouldPublishNotificationToSubscribers()
    {
        // Arrange
        var receiverMock = new Mock<IReceiver>();
        
        var mediator = GetMediator(
            services => services.AddSingleton(sp => receiverMock.Object),
            cfg => cfg.AddNotificationHandler<NotificationHandler>());
        
        // Act
        await mediator.PublishAsync(new TestNotification("Hi"));
        
        // Assert
        receiverMock.Verify(x => x.Receive("Hi"), Times.Once);
    }
    
    public interface IReceiver
    {
        void Receive(string message);
    }
    
    public class MessageHandler : IRequestHandler<TestMessage, TestResponse>
    {
        public Task<TestResponse> HandleAsync(TestMessage request, CancellationToken cancellationToken = default)
        {
            var response = new TestResponse(Guid.NewGuid(), "TEST");

            return Task.FromResult(response);
        }
    }

    public class MessageWithoutResponseHandler(IReceiver receiver) : IRequestHandler<TestMessageWithoutResponse>
    {
        public Task HandleAsync(TestMessageWithoutResponse request, CancellationToken cancellationToken = default)
        {
            receiver.Receive(request.Value);
            return Task.CompletedTask;
        }
    }

    public class NotificationHandler(IReceiver receiver) : INotificationHandler<TestNotification>
    {
        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            receiver.Receive(notification.Value);
            return Task.CompletedTask;
        }
    }
    
    public record TestMessage(string Value) : IRequest<TestResponse>;

    public record TestMessageWithoutResponse(string Value) : IRequest;

    public record TestResponse(Guid Id, string Value);
    
    public record TestNotification(string Value) : INotification;

    private static IMediator GetMediator(Action<IServiceCollection> configureServices, Action<MediatorOptions> configureMediator)
    {
        var services = new ServiceCollection();
        configureServices(services);
        services.AddMediator(configureMediator);
        
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        
        return mediator;
    }
}