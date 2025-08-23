using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.RabbitMq;

public class RabbitMqRpcClientAttribute() : FromKeyedServicesAttribute(RabbitMqOptions.TransportName);