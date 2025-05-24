using Avs.Messaging.Contracts;
using Avs.Messaging.RabbitMq;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Avs.Messaging.Tests.RabbitMq;

public class PubSubTests : RabbitMqTestsBase
{
    [Test]
    public async Task Consumer_ShouldReceivePublishedMessage()
    {
       // Arrange
       using var producer = CreateProducerHost();
       using var consumer1 = CreateConsumerHost(); 
       using var consumer2 = CreateConsumerHost();
        
       await Task.WhenAll(producer.StartAsync(), consumer1.StartAsync(), consumer2.StartAsync());
       
       var producerPublisher = producer.Services.GetRequiredService<IMessagePublisher>();
       var consumer1Verifier = consumer1.Services.GetRequiredService<IMessageVerifier>();
       var consumer2Verifier = consumer2.Services.GetRequiredService<IMessageVerifier>();
       
       // Act
       var message = new Greeting() { Message = "Hello" };
       await producerPublisher.PublishAsync(message);

       var message1 = await consumer1Verifier.GetMessageAsync() as Greeting;
       var message2 = await consumer2Verifier.GetMessageAsync() as Greeting;
       
       // Assert
       Assert.Multiple(() =>
       {
           Assert.That(message1, Is.Not.Null);
           Assert.That(message2, Is.Not.Null);
           Assert.That(message1!.Message, Is.EqualTo(message.Message));
           Assert.That(message1!.Time, Is.EqualTo(message.Time));
           Assert.That(message2!.Message, Is.EqualTo(message.Message));
           Assert.That(message2!.Time, Is.EqualTo(message.Time));
       });
    }
    
    private IHost CreateConsumerHost()
    {
        return TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddMessaging(x =>
            {
                x.AddConsumer<GreetingConsumer>();
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