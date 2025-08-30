using AccountService.Endpoints;
using AccountService.Services;
using Avs.Messaging;
using Avs.Messaging.RabbitMq;
using Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAccountService, AccountServiceImpl>();
builder.Services.AddMessaging(options =>
{
    options.UseRabbitMq(cfg =>
    {
        cfg.Host = builder.Configuration.GetValue<string>("RabbitMQ:Host")!;
        cfg.Port = builder.Configuration.GetValue<int>("RabbitMQ:Port");
        
        cfg.AddConsumer<SupervisorResponseConsumer>();
        cfg.ConfigureRequestReply<SupervisorRequest, SupervisorResponse>();
        cfg.AddRpcClient();
    });
});

var app = builder.Build();

app.MapGet("/", () => "Account service is running...");
app.MapAccountEndpoint();

app.Run();