namespace Avs.Messaging;

/// <summary>
/// Represents a container for task completion source in Request-Reply flow.
/// </summary>
/// <param name="TaskCompletionSource">Task completion source to store</param>
/// <param name="ResponseType">Type of response</param>
public record RequestReplyInfo(TaskCompletionSource<object?> TaskCompletionSource, Type ResponseType);