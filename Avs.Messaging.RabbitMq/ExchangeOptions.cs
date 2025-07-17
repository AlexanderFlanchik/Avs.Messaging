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
    /// Exchange type (Fanout by default)
    /// </summary>
    public string ExchangeType { get; internal set; } = RabbitMQ.Client.ExchangeType.Fanout;

    /// <summary>
    /// Basic properties (headers, correlation ID, etc.
    /// </summary>
    public BasicProperties? Props { get; set; } = default;
    
    /// <summary>
    /// Checks if exchange is request-reply channel
    /// </summary>
    public bool IsRequestReply { get; set; }
    
    /// <summary>
    /// For a request-reply flow, this is a type of message which represents request.
    /// </summary>
    public string? RequestType { get; set; }
    
    /// <summary>
    /// Sets exchange type to Topic
    /// </summary>
    public void SetTopicExchange() => ExchangeType = RabbitMQ.Client.ExchangeType.Topic;
    
    /// <summary>
    /// Sets exchange type to Direct
    /// </summary>
    public void SetDirectExchange() => ExchangeType = RabbitMQ.Client.ExchangeType.Direct;
    
    /// <summary>
    /// Sets exchange type to Headers
    /// </summary>
    public void SetHeadersExchange() => ExchangeType = RabbitMQ.Client.ExchangeType.Headers;
}