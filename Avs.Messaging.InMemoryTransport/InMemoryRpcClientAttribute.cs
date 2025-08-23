using Avs.Messaging.InMemoryTransport;
using Microsoft.Extensions.DependencyInjection;

namespace Avs.Messaging.InMemoryTransport;

public sealed class InMemoryRpcClientAttribute(): FromKeyedServicesAttribute(InMemoryTransportOptions.TransportName);