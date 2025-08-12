using Avs.Messaging.Contracts;
using Avs.Messaging.InMemoryTransport;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Avs.Messaging.Tests.InMemory;

public class FilterTests
{
    private Mock<IFilterVerifier> _filterVerifierMock;

    [SetUp]
    public void Setup()
    {
        _filterVerifierMock = new Mock<IFilterVerifier>();
    }
    
    [Test]
    public async Task Consumer_ShouldApplyAddedFilter()
    {
        // Arrange
        using var host = CreateHost();
        
        await host.StartAsync();
       
        var publisher = host.Services.GetRequiredService<IMessagePublisher>();
        var consumer = host.Services.GetRequiredService<IMessageVerifier>();

        string initalMessage = "Hello", finalMessage = "HELLO";
       
        // Act
        var message = new Greeting() { Message = initalMessage };
        await publisher.PublishAsync(message);

        var messageConsumed = await consumer.GetMessageAsync() as Greeting;
       
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(messageConsumed, Is.Not.Null);
            Assert.That(messageConsumed!.Message, Is.EqualTo(finalMessage));
            Assert.That(messageConsumed!.Time, Is.EqualTo(message.Time));
            _filterVerifierMock.Verify(x => x.VerifyBeforeAction(initalMessage), Times.Once);
            _filterVerifierMock.Verify(x => x.VerifyAfterAction(finalMessage), Times.Once);
        });
    }
    
    private IHost CreateHost()
    {
        return TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddScoped<IFilterVerifier>(sp => _filterVerifierMock.Object);
            services.AddMessaging(x =>
            {
                x.AddConsumer<GreetingConsumer>();
                x.AddMessageFilter<Greeting, DummyFilter>();
                x.UseInMemoryTransport();
            });
        }).Build();
    }
}