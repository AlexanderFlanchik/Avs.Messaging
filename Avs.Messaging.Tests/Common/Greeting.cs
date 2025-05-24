namespace Avs.Messaging.Tests.Common;

public class Greeting
{
    public string Message { get; set; } = default!;
    public DateTime Time { get; set; } = DateTime.UtcNow;
}