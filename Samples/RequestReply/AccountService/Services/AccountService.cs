using AccountService.Models;
using Avs.Messaging.Contracts;
using Avs.Messaging.RabbitMq;
using Contracts;

namespace AccountService.Services;

public interface IAccountService
{
    Task<Guid> CreateAccountAsync(CreateAccount account);
}

public class AccountServiceImpl([RabbitMqRpcClient] IRpcClient rpcClient, ILogger<AccountServiceImpl> logger) : IAccountService
{
    public async Task<Guid> CreateAccountAsync(CreateAccount account)
    {
        var supervisorRequest = new SupervisorRequest()
        {
            LocationId = account.LocationId,
            RequestId = Guid.NewGuid()
        };
        
        // RPC call
        var supervisorResponse = await rpcClient.RequestAsync<SupervisorRequest, SupervisorResponse>(supervisorRequest);
        logger.LogInformation("Supervisor request ID: {requestId}, response: {supervisorId}", 
            supervisorRequest.RequestId,
            supervisorResponse.SupervisorId);
        
        var newAccount = new Account()
        {
            Id = Guid.NewGuid(),
            FirstName = account.FirstName,
            LastName = account.LastName,
            Email = account.Email,
            SupervisorId = supervisorResponse.SupervisorId,
        };
        
        return newAccount.Id;
    }
}
