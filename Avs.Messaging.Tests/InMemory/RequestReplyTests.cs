using Avs.Messaging.Contracts;
using Avs.Messaging.InMemoryTransport;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.Tests.InMemory;

public class RequestReplyTests
{
    [Test]
    public async Task Consumer_ShouldReplyToRequest()
    {
        // Arrange
        using var host = TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddMessaging(x =>
            {
                x.AddConsumer<PingConsumer>();
                x.AddConsumer<PongConsumer>();
                x.UseInMemoryTransport(o => o.AddRpcClient());
            });
        }).Build();
        
        await host.StartAsync();
        await host.StartAsync();
        
        using var scope = host.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredKeyedService<IRpcClient>(InMemoryTransportOptions.TransportName);
        var ping = new Ping(Guid.NewGuid(), DateTime.UtcNow);
        
        // Act
        var pong = await client.RequestAsync<Ping, Pong>(ping);
        
        // Assert
        Assert.That(pong, Is.Not.Null);
        Assert.That(pong.Id, Is.EqualTo(ping.Id));
    }
}