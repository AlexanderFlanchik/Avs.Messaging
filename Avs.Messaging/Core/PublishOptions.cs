namespace Avs.Messaging.Core;

/// <summary>
/// Represents options for message publish
/// </summary>
public class PublishOptions
{
    /// <summary>
    /// Correlation ID of message to publish
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Checks if the publish is part of request-reply flow
    /// </summary>
    public bool IsRequestReply { get; set; }
    
    /// <summary>
    /// Message headers  collection
    /// </summary>
    public IDictionary<string, object?>? Headers { get; set; }
}