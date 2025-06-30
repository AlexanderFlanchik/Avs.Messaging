using System.Text;
using System.Text.Json;
using Avs.Messaging;
using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.RabbitMq;
using Contracts;
using MessageConsumer.Components;
using MessageConsumer.Services;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var rabbitmqHost = builder.Configuration["RabbitMQ:Host"];
var rabbitmqPort = builder.Configuration.GetValue<int>("RabbitMQ:Port");

builder.Services.AddRazorComponents();
builder.Services.AddScoped<HtmlRenderService>();
builder.Services.AddSingleton<ChannelManager>();

builder.Services.AddMessaging(x =>
{
    x.AddConsumer<NewUserConsumer>();
    x.UseRabbitMq(cfg =>
    {
        cfg.Host = rabbitmqHost!;
        cfg.Port = rabbitmqPort;
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.MapGet("/", () => new RazorComponentResult<Home>());

app.MapGet("/new-users/{instanceId}",
    async (Guid instanceId, ChannelManager channelManager, HttpContext context, HtmlRenderService renderer, CancellationToken cancellationToken) =>
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.Append("Connection", "keep-alive");
        
        var channel = channelManager.GetChannel(instanceId);
        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var messageContent = await renderer.RenderComponent<MessageRow>(
                    new Dictionary<string, object?>()
                    {
                        ["Data"] = message
                    });

                var payload = $"event: message\ndata: {messageContent?.Replace("\n", string.Empty)}\n\n";
                var bytes = Encoding.UTF8.GetBytes(payload);

                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await context.Response.Body.FlushAsync();
            }
        }
        finally
        {
            channelManager.CloseChannel(instanceId);
        }
    });

app.Run();

/// <summary>
/// Simple consumer
/// </summary>
/// <param name="channelManager">Channel manager for accessing all active channels
/// used for communication with /new-users endpoint</param>
/// <param name="logger">Logger</param>
class NewUserConsumer(ChannelManager channelManager, ILogger<NewUserConsumer> logger) : ConsumerBase<NewUser>
{
    protected override Task Consume(MessageContext<NewUser> messageContext)
    {
        logger.LogInformation(JsonSerializer.Serialize(messageContext.Message));
        foreach (var channel in channelManager.Channels)
        {
            channel.Writer.TryWrite(messageContext.Message);
        }

        return Task.CompletedTask;
    }
}