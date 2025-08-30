using AccountService.Services;
using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Endpoints;

public static class AccountEndpointExtensions
{
    public static IEndpointRouteBuilder MapAccountEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/accounts");
        group.MapPost("/", 
            async ([FromServices] IAccountService accountService, CreateAccount account) =>
            {
                var accountId = await accountService.CreateAccountAsync(account);
                
                return Results.Ok(accountId);
            });
        
        return endpoints;
    }
}