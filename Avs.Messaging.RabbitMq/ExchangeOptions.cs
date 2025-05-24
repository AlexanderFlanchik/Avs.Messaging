using RabbitMQ.Client;

namespace Avs.Messaging.RabbitMq;

/// <summary>
/// Represents options for RabbitMQ queue consumer/publisher
/// </summary>
public class ExchangeOptions
{
    /// <summary>
    /// Checks if consumer queue is durable
    /// </summary>
    public bool IsQueueDurable { get; set; }
    
    /// <summary>
    /// Checks if consumer queue is exclusive 
    /// </summary>
    public bool IsQueueExclusive { get; set; }

    /// <summary>
    /// Exchange name
    /// </summary>
    public string? ExchangeName { get; set; } = default;
    
    /// <summary>
    /// Checks if exchange is durable
    /// </summary>
    public bool IsExchangeDurable { get; set; }
    
    /// <summary>
    /// Queue name. If not specified, the type of message (+ service ID if queue is durable) should be used
    /// </summary>
    public string QueueName { get; set; } = default!;

    /// <summary>
    /// Routing key if specified. Is ignored if exchange type is fan-out
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Exchange type
    /// </summary>
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;

    /// <summary>
    /// Basic properties (headers, correlation ID, etc.
    /// </summary>
    public BasicProperties? Props { get; set; } = default;
    
    /// <summary>
    /// Checks if exchange is request-reply channel
    /// </summary>
    public bool IsRequestReply { get; set; }
    
    public string? RequestType { get; set; }
}