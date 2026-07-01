using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.RabbitMq;
using Avs.Messaging.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Avs.Messaging.Tests.RabbitMq;

public class RequestReplyTests : RabbitMqTestsBase
{
    [Test]
    public void ConfigureRequestReply_ShouldUseDurableDirectExchangeSettings()
    {
        var options = new RabbitMqOptions();

        options.ConfigureRequestReply<Ping, Pong>();

        var requestSettings = options.ExchangeSettings[typeof(Ping)];
        var responseSettings = options.ExchangeSettings[typeof(Pong)];

        Assert.Multiple(() =>
        {
            Assert.That(requestSettings.IsRequestReply, Is.True);
            Assert.That(requestSettings.IsQueueDurable, Is.True);
            Assert.That(requestSettings.IsExchangeDurable, Is.True);
            Assert.That(requestSettings.ExchangeName, Is.EqualTo(typeof(Ping).FullName));
            Assert.That(requestSettings.RoutingKey, Is.EqualTo(typeof(Ping).FullName));
            Assert.That(requestSettings.ExchangeType, Is.EqualTo(ExchangeType.Direct));

            Assert.That(responseSettings.IsRequestReply, Is.True);
            Assert.That(responseSettings.IsQueueDurable, Is.True);
            Assert.That(responseSettings.IsExchangeDurable, Is.True);
            Assert.That(responseSettings.ExchangeName, Is.EqualTo(typeof(Pong).FullName));
            Assert.That(responseSettings.RoutingKey, Is.EqualTo(typeof(Pong).FullName));
            Assert.That(responseSettings.ExchangeType, Is.EqualTo(ExchangeType.Direct));
        });
    }

    [Test]
    public async Task Publisher_ShouldHandleExistingNonDurableExchange()
    {
        using var host = TestHostBuilder.CreateTestHostBuilder(services =>
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

        await host.StartAsync();

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = RabbitMqContainer.Hostname,
                Port = RabbitMqContainer.GetMappedPublicPort(5672),
                UserName = Guest,
                Password = Guest
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(typeof(Greeting).FullName!, ExchangeType.Fanout, durable: false, autoDelete: false);

            var publisher = host.Services.GetRequiredService<IMessagePublisher>();

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(new Greeting { Message = "Hello" }));
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Test]
    public async Task ReplyMessage_WithUnknownCorrelationId_ShouldBeRejectedForRedelivery()
    {
        const string queueName = "reply-test-queue";
        var state = new TrackingConsumerState();

        using var host = TestHostBuilder.CreateTestHostBuilder(services =>
        {
            services.AddSingleton(state);
            services.AddMessaging(x =>
            {
                x.AddConsumer<TrackingGreetingConsumer>();
                x.UseRabbitMq(cfg =>
                {
                    cfg.Host = RabbitMqContainer.Hostname;
                    cfg.Port = RabbitMqContainer.GetMappedPublicPort(5672);
                    cfg.Username = Guest;
                    cfg.Password = Guest;
                    cfg.ServiceId = "reply-test-service";
                    cfg.ConfigureExchangeOptions<Greeting>(o =>
                    {
                        o.QueueName = queueName;
                        o.IsQueueDurable = true;
                        o.IsExchangeDurable = true;
                    });
                });
            });
        }).Build();

        await host.StartAsync();

        try
        {
            var publisher = host.Services.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync(new Greeting { Message = "Hello" }, new PublishOptions
            {
                CorrelationId = "unknown-correlation",
                IsRequestReply = true
            });

            await Task.Delay(1000);

            var factory = new ConnectionFactory
            {
                HostName = RabbitMqContainer.Hostname,
                Port = RabbitMqContainer.GetMappedPublicPort(5672),
                UserName = Guest,
                Password = Guest
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            var queue = await channel.QueueDeclareAsync($"{queueName}_reply-test-service", durable: true, exclusive: false, autoDelete: false, arguments: null, passive: true);

            Assert.That(queue.MessageCount, Is.GreaterThan(0));
            Assert.That(state.Count, Is.EqualTo(0));
        }
        finally
        {
            await host.StopAsync();
        }
    }

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

    private sealed class TrackingConsumerState
    {
        public int Count;
    }

    private sealed class TrackingGreetingConsumer(TrackingConsumerState state) : ConsumerBase<Greeting>
    {
        protected override Task Consume(MessageContext<Greeting> messageContext)
        {
            Interlocked.Increment(ref state.Count);
            return Task.CompletedTask;
        }
    }
}