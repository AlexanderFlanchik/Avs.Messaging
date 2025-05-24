using Avs.Messaging.Core;

namespace Avs.Messaging.Contracts;

public interface IConsumer
{
    /// <summary>
    /// Consumes an abstract message using provided <see cref="ConsumerContext"/> context
    /// </summary>
    /// <param name="context">Consumer context</param>
    /// <returns>A task which resolves when a message is handled.</returns>
    Task Consume(ConsumerContext context);
}