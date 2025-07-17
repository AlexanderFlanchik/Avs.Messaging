using System.Threading.Channels;

namespace Avs.Messaging.Tests.Common;

public class MessageVerifier : IMessageVerifier
{
    private readonly Channel<object> _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(100));
    
    public async Task<object?> GetMessageAsync()
    {
        object? result = null;
        Task[] tasks = [Task.Run(async () => result = await _channel.Reader.ReadAsync()), Task.Delay(200)];
        await Task.WhenAny(tasks);
        
        return result;
    }

    public void SetMessage(object message)
    {
        _channel.Writer.TryWrite(message);    
    }
}