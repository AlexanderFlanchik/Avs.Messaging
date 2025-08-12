namespace Avs.Messaging.Core;

public interface IMessageHandleFilter<T>
{
    /// <summary>
    /// Message handle filter. Can stop message handle propagation or continue using next step invocation.
    /// </summary>
    /// <param name="context">Message context</param>
    /// <param name="next">The next step in message handle pipeline</param>
    /// <returns>A task which represents an async operation</returns>
    Task HandleAsync(MessageContext<T> context, MessageHandlerDelegate<T> next);
}