using Avs.Messaging;
using Avs.Messaging.Contracts;
using Avs.Messaging.RabbitMq;
using Contracts;

var builder = WebApplication.CreateBuilder(args);
var rabbitmqHost = builder.Configuration["RabbitMQ:Host"];
var rabbitmqPort = builder.Configuration.GetValue<int>("RabbitMQ:Port");
builder.Services.AddMessaging(x =>
{
    x.UseRabbitMq(cfg =>
    {
        cfg.Host = rabbitmqHost!;
        cfg.Port = rabbitmqPort;
    });
});
var app = builder.Build();

app.MapGet("/", () => "Message Producer is running...");

app.MapPost("/new-user", async (IMessagePublisher publisher, NewUser user) =>
{
    user.UserId = Guid.NewGuid();
    await publisher.PublishAsync(user);

    return Results.Accepted();
});

app.Run();

