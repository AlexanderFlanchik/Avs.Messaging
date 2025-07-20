using Avs.Messaging;
using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.InMemoryTransport;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMessaging(x =>
{
    x.AddConsumer<RegisterCommandConsumer>();
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddRpcClient();
    x.UseInMemoryTransport();
});

var app = builder.Build();

app.MapGet("/", () => "Mediator service is running...");
app.MapPost("/register", async (IRpcClient client, RegisterCommand command, CancellationToken cancellationToken) =>
{
    var response = await client.RequestAsync<RegisterCommand, UserRegistered>(command, cancellationToken);
    return Results.Ok(response);
});

app.Run();

record RegisterCommand(string FirstName, string LastName, string Email, string Password);
record UserRegistered(Guid Id);

class RegisterCommandConsumer(ILogger<RegisterCommandConsumer> logger) : ConsumerBase<RegisterCommand>
{
    protected override async Task Consume(MessageContext<RegisterCommand> messageContext)
    {
        var message = messageContext.Message;
        logger.LogInformation("Received register command: {firstName}, {lastName}, {email}",
            message.FirstName,
            message.LastName,
            message.Email);
        
        var registerId = Guid.NewGuid();

        await RespondAsync(new UserRegistered(registerId), messageContext);
    }
}

class UserRegisteredConsumer : ConsumerBase<UserRegistered>
{
    protected override Task Consume(MessageContext<UserRegistered> messageContext)
    {
        return Task.CompletedTask;
    }
}