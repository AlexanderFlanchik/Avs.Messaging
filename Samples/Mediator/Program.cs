using Avs.Messaging;
using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Avs.Messaging.InMemoryTransport;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMessaging(x =>
{
    x.AddConsumer<RegisterCommandConsumer>();
    x.AddConsumer<UserRegisteredConsumer>();
    x.UseInMemoryTransport(o => o.AddRpcClient());
});

builder.Services.AddScoped<IMediator, Mediator>();

var app = builder.Build();

app.MapGet("/", () => "Mediator service is running...");
app.MapPost("/register", async (IMediator mediator, RegisterCommand command, CancellationToken cancellationToken) =>
{
    var response = await mediator.SendAsync<RegisterCommand, UserRegistered>(command, cancellationToken);
    return Results.Ok(response);
});

app.Run();

// Contracts
record RegisterCommand(string FirstName, string LastName, string Email, string Password);
record UserRegistered(Guid Id);

// Mediator (App layer)
interface IMediator
{
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken);
}

class Mediator([InMemoryRpcClient]IRpcClient client) : IMediator
{
    public Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        => client.RequestAsync<TRequest, TResponse>(request, cancellationToken);
}

// Handler implementation
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

// Still needed by RPC flow
class UserRegisteredConsumer : ConsumerBase<UserRegistered>
{
    protected override Task Consume(MessageContext<UserRegistered> messageContext)
    {
        return Task.CompletedTask;
    }
}