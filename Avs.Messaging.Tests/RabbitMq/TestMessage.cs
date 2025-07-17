namespace Avs.Messaging.Tests.RabbitMq;

public class TestMessage : IEquatable<TestMessage>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = default!;
    
    public bool Equals(TestMessage? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return Id == other.Id && string.Equals(Message, other.Message);
    }
}