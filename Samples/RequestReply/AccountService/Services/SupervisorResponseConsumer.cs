using Avs.Messaging.Contracts;
using Avs.Messaging.Core;
using Contracts;

namespace AccountService.Services;

public class SupervisorResponseConsumer : ConsumerBase<SupervisorResponse>
{
    protected override Task Consume(MessageContext<SupervisorResponse> messageContext) => Task.CompletedTask;
}