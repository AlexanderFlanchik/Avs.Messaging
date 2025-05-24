namespace Avs.Messaging.Tests.Common;

public interface IMessageVerifier
{
    Task<object?> GetMessageAsync();
    void SetMessage(object message);
}