namespace Avs.Messaging;

/// <summary>
/// Represents an error during request-reply execution
/// </summary>
/// <param name="errorType">Type of error</param>
/// <param name="errorMessage">Error details</param>
public class RequestReplyException(RequestReplyError errorType, string errorMessage) : Exception(errorMessage)
{
    public RequestReplyError Type { get; init; } = errorType;
}

public enum RequestReplyError
{
    HandlerError,
    TooManyRequests,
    Cancelled,
}