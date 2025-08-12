using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.RabbitMq;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Avs.Messaging.Tests.RabbitMq;

public class FilterTests : RabbitMqTestsBase
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
        using var producer = CreateProducerHost();
        using var consumer = CreateConsumerHost(); 
        
        await Task.WhenAll(producer.StartAsync(), consumer.StartAsync());
       
        var producerPublisher = producer.Services.GetRequiredService<IMessagePublisher>();
        var consumerVerifier = consumer.Services.GetRequiredService<IMessageVerifier>();

        string initalMessage = "Hello", finalMessage = "HELLO";
       
        // Act
        var message = new Greeting() { Message = initalMessage };
        await producerPublisher.PublishAsync(message);

        var messageConsumed = await consumerVerifier.GetMessageAsync() as Greeting;
       
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
    
    private IHost CreateConsumerHost()
    {
        return TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddScoped<IFilterVerifier>(sp => _filterVerifierMock.Object);
            services.AddMessaging(x =>
            {
                x.AddConsumer<GreetingConsumer>();
                x.AddMessageFilter<Greeting, DummyFilter>();
                x.UseRabbitMq(cfg =>
                {
                    cfg.Host = RabbitMqContainer.Hostname;
                    cfg.Port = RabbitMqContainer.GetMappedPublicPort(5672);
                    cfg.Username = Guest;
                    cfg.Password = Guest;
                });
            });
        }).Build();
    }
    
    private IHost CreateProducerHost()
    {
        return TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddMessaging(x =>
            {
                x.UseRabbitMq(cfg =>
                {
                    cfg.Host = RabbitMqContainer.Hostname;
                    cfg.Port = RabbitMqContainer.GetMappedPublicPort(5672);
                    cfg.Username = Guest;
                    cfg.Password = Guest;
                });
            });
        }).Build();
    }
}