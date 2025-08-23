using Avs.Messaging.Core;

// ReSharper disable All

namespace Avs.Messaging.RabbitMq;

public class RabbitMqOptions : TransportOptionsBase
{
    public const string TransportName = "RabbitMq";
    
    /// <summary>
    /// RabbitMQ broker host
    /// </summary>
    public string Host { get; set; } = default!;
    
    /// <summary>
    /// RabbitMq port
    /// </summary>
    public int Port { get; set; }
    
    /// <summary>
    /// User
    /// </summary>
    public string Username { get; set; } = default!;
    
    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = default!;
    
    /// <summary>
    /// Client service ID. Used as a part of durable queues
    /// </summary>
    public string ServiceId { get; set; } = default!;
    
    
    /// <summary>
    /// A queue and exchange name for errors during request-reply handling
    /// </summary>
    public string RequestReplyErrorQueue { get; set; } = "rpc-error";
    
    /// <summary>
    /// Exchange settings
    /// </summary>
    public IDictionary<Type, ExchangeOptions> ExchangeSettings { get; } = new Dictionary<Type, ExchangeOptions>();

    /// <summary>
    /// Configures exchange options for specific message type
    /// </summary>
    /// <param name="configure">A delegate that configures exchange options</param>
    /// <typeparam name="T">Message type</typeparam>
    public void ConfigureExchangeOptions<T>(Action<ExchangeOptions> configure) where T : class
    {
        ConfigureExchangeOptionsInternal(typeof(T), configure);
    }

    /// <summary>
    /// Configures a request-reply exchange
    /// </summary>
    /// <typeparam name="TRequest">Type of request</typeparam>
    /// <typeparam name="TResponse">Type of response</typeparam>
    public void ConfigureRequestReply<TRequest, TResponse>() where TRequest : class
                                                             where TResponse : class
    {
        ConfigureExchangeOptions<TRequest>(o =>
        {
            o.IsRequestReply = true;
            o.RoutingKey = typeof(TRequest).FullName!;
            o.SetDirectExchange();
        });
                
        ConfigureExchangeOptions<TResponse>(o =>
        {
            o.IsRequestReply = true;
            o.SetDirectExchange();
            o.RoutingKey = typeof(TResponse).FullName!;
            o.RequestType = typeof(TRequest).FullName!;
        });
    }
    
    private void ConfigureExchangeOptionsInternal(Type messageType, Action<ExchangeOptions> configure)
    {
        var consumerOptions = new ExchangeOptions();
        configure(consumerOptions);
        
        if (string.IsNullOrEmpty(consumerOptions.ExchangeName))
        {
            consumerOptions.ExchangeName = messageType.FullName!;
        }
        
        ExchangeSettings[messageType] =  consumerOptions;
    }
}