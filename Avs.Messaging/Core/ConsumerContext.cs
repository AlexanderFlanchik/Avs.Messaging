using Avs.Messaging.Contracts;

namespace Avs.Messaging.Core;

public class ConsumerContext
{
    public object Message { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public IDictionary<string, object?>? Headers { get; set; } = new Dictionary<string, object?>();
    public IMessagePublisher MessagePublisher { get; set; } = default!;

    public IEnumerable<object?> Filters { get; set; } = [];
}