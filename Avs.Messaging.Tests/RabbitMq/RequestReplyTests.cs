using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.RabbitMq;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Avs.Messaging.Tests.RabbitMq;

public class RequestReplyTests : RabbitMqTestsBase
{
    [Test]
    public async Task Consumer_ShouldReplyToRequest()
    {
        // Arrange
        using var requestHost = CreateTestHost(x =>
        {
            x.AddConsumer<PongConsumer>();
        });

        using var replierHost = CreateTestHost(x => x.AddConsumer<PingConsumer>());

        await Task.WhenAll(requestHost.StartAsync(), replierHost.StartAsync());

        try
        {
            using var scope = requestHost.Services.CreateScope();
            var client = scope.ServiceProvider.GetRequiredKeyedService<IRpcClient>(RabbitMqOptions.TransportName);
            var ping = new Ping(Guid.NewGuid(), DateTime.UtcNow);

            // Act
            var pong = await client.RequestAsync<Ping, Pong>(ping);
            
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(pong, Is.Not.Null);
                Assert.That(pong.Id, Is.EqualTo(ping.Id));
            });
        }
        finally
        {
            await requestHost.StopAsync();
            await replierHost.StopAsync();
        }
    }

    private IHost CreateTestHost(Action<MessagingOptions> configure)
    {
        return TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddMessaging(x =>
            {
                configure(x);
                x.UseRabbitMq(cfg =>
                {
                    cfg.Host = RabbitMqContainer.Hostname;
                    cfg.Port = RabbitMqContainer.GetMappedPublicPort(5672);
                    cfg.Username = Guest;
                    cfg.Password = Guest;
                    cfg.ConfigureRequestReply<Ping, Pong>();
                    cfg.AddRpcClient();
                });
            });
        }).Build();
    }
}