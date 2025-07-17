using Avs.Messaging.Contracts;
using Avs.Messaging.RabbitMq;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Avs.Messaging.Tests.RabbitMq;

public class TopicTests : RabbitMqTestsBase
{
    private const string ExchangeName = "test-exchange";
    private const string TestTopic = "test-topic";

    [Test]
    public async Task Consumers_ShouldReceiveMessageFromTopic_IfRoutingKeyMatches()
    {
        // Arrange
        string routingKey1 = $"{TestTopic}.#"; // matches producer routing key
        string routingKey2 = $"{TestTopic}.second.#"; // does not match
        string routingKey3 = "#"; // allows all
        
        using var producer = CreateProducerHost();
        using var consumer1 = CreateConsumerHost(routingKey1);
        using var consumer2 = CreateConsumerHost(routingKey2);
        using var consumer3 = CreateConsumerHost(routingKey3);
        
        await Task.WhenAll(producer.StartAsync(), consumer1.StartAsync(), consumer2.StartAsync(), consumer3.StartAsync());
        var producerPublisher = producer.Services.GetRequiredService<IMessagePublisher>();
        
        // Act
        var message = new TestMessage() { Message = "This is a test" };
        await producerPublisher.PublishAsync(message);
        
        var consumer1Verifier = consumer1.Services.GetRequiredService<IMessageVerifier>();
        var message1 = await consumer1Verifier.GetMessageAsync() as TestMessage;
        
        var consumer2Verifier = consumer2.Services.GetRequiredService<IMessageVerifier>();
        var message2 = await consumer2Verifier.GetMessageAsync() as TestMessage;
        
        var consumer3Verifier = consumer3.Services.GetRequiredService<IMessageVerifier>();
        var message3 = await consumer3Verifier.GetMessageAsync() as TestMessage;

        // Assert
        try
        {
            Assert.Multiple(() =>
            {
                // First consumer should receive the message
                Assert.That(message1, Is.EqualTo(message));
                
                // Second one should not
                Assert.That(message2, Is.Null);
                
                // And third also should receive
                Assert.That(message3, Is.EqualTo(message));
            });
        }
        finally
        {
            await consumer1.StopAsync();
            await consumer2.StopAsync();
            await consumer3.StopAsync();
            await producer.StopAsync();
        }
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
                    cfg.ConfigureExchangeOptions<TestMessage>(o =>
                    {
                        o.ExchangeName = ExchangeName;
                        o.RoutingKey = $"{TestTopic}.consumer";
                        o.SetTopicExchange();
                    });
                });
            });
        }).Build();
    }
    
    private IHost CreateConsumerHost(string routingKey)
    {
        return TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddMessaging(x =>
            {
                x.AddConsumer<TestMessageConsumer>();
                x.UseRabbitMq(cfg =>
                {
                    cfg.Host = RabbitMqContainer.Hostname;
                    cfg.Port = RabbitMqContainer.GetMappedPublicPort(5672);
                    cfg.Username = Guest;
                    cfg.Password = Guest;
                    cfg.ConfigureExchangeOptions<TestMessage>(o =>
                    {
                        o.ExchangeName = ExchangeName;
                        o.RoutingKey = routingKey;
                        o.SetTopicExchange();
                    });
                });
            });
        }).Build();
    }
}