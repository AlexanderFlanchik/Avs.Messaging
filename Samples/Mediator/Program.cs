using Avs.Messaging.Mediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator(cfg => cfg.AddRequestHandler<RegisterCommand, RegisterCommandHandler>());
var app = builder.Build();

app.MapGet("/", () => "Mediator service is running...");

app.MapPost("/register", async (IMediator mediator, RegisterCommand command, CancellationToken cancellationToken) =>
{
    var response = await mediator.SendAsync<RegisterCommand, UserRegistered>(command, cancellationToken);
    return Results.Ok(response);
});

app.Run();

// Contracts
record RegisterCommand(string FirstName, string LastName, string Email, string Password) : IRequest<UserRegistered>;
record UserRegistered(Guid Id);

class RegisterCommandHandler(ILogger<RegisterCommandHandler> logger) : IRequestHandler<RegisterCommand, UserRegistered>
{
    public Task<UserRegistered> HandleAsync(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"Registered user: {request.FirstName} {request.LastName}");
        return Task.FromResult(new UserRegistered(Guid.NewGuid()));
    }
}