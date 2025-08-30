using Avs.Messaging;
using Avs.Messaging.RabbitMq;
using Contracts;
using SupervisorService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMessaging(options =>
{
    options.UseRabbitMq(cfg =>
    {
        cfg.Host = builder.Configuration.GetValue<string>("RabbitMQ:Host")!;
        cfg.Port = builder.Configuration.GetValue<int>("RabbitMQ:Port");
        cfg.AddConsumer<SupervisorRequestConsumer>();
        cfg.ConfigureRequestReply<SupervisorRequest, SupervisorResponse>();
    });
});

var host = builder.Build();
host.Run();