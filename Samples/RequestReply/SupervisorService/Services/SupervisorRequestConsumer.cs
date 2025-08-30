using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Contracts;

namespace SupervisorService.Services;

public class SupervisorRequestConsumer(ILogger<SupervisorRequestConsumer> logger) : ConsumerBase<SupervisorRequest>
{
    protected override async Task Consume(MessageContext<SupervisorRequest> messageContext)
    {
        logger.LogInformation("Received Supervisor request ID: {requestId}", messageContext.Message.RequestId);
        var supervisorId = Guid.NewGuid(); // We get some supervisor ID from database here

        var response = new SupervisorResponse()
        {
            RequestId = messageContext.Message.RequestId,
            SupervisorId = supervisorId
        };

        await RespondAsync(response, messageContext);
    }
}