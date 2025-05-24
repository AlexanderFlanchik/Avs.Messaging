using Avs.Messaging.Contracts;
using Avs.Messaging.InMemoryTransport;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.Tests.InMemory;

public class PubSubTests
{
    [Test]
    public async Task Consumer_ShouldReceivePublishedMessage()
    {
        // Arrange
        using var host = TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddMessaging(x =>
            {
                x.AddConsumer<GreetingConsumer>();
                x.UseInMemoryTransport();
            });
        }).Build();
        
        await host.StartAsync();
        
        var message = new Greeting() { Message = "Hello" };
        var publisher = host.Services.GetRequiredService<IMessagePublisher>();
        var verifier = host.Services.GetRequiredService<IMessageVerifier>();
       
        // Act
        await publisher.PublishAsync(message);
        Greeting? actualMessage = await verifier.GetMessageAsync() as Greeting;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(actualMessage is not null);
            Assert.That(actualMessage!.Message, Is.EqualTo(message.Message));
            Assert.That(actualMessage!.Time, Is.EqualTo(message!.Time));
        });
    }
}