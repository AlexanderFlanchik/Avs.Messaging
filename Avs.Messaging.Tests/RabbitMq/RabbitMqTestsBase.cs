using DotNet.Testcontainers.Builders;
using Testcontainers.RabbitMq;

namespace Avs.Messaging.Tests.RabbitMq;

public abstract class RabbitMqTestsBase
{
    protected const string Guest = "guest";
    
    protected readonly RabbitMqContainer RabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithCleanUp(true)
        .WithName($"test-rabbitmq-{Guid.NewGuid()}")
        .WithPortBinding(5672, true) // Map random port
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
        .WithUsername(Guest)
        .WithPassword(Guest)
        .Build();
    
    [OneTimeSetUp]
    public async Task BeforeAll()
    {
        await RabbitMqContainer.StartAsync();
    }
    
    [OneTimeTearDown]
    public async Task Clear()
    {
        await RabbitMqContainer.DisposeAsync();
    }
}