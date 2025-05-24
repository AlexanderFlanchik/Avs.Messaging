using System.Threading.Channels;

namespace Avs.Messaging.Tests.Common;

public class MessageVerifier : IMessageVerifier
{
    private readonly Channel<object> _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(100));
    
    public async Task<object?> GetMessageAsync()
    {
        return await _channel.Reader.ReadAsync();
    }

    public void SetMessage(object message)
    {
        _channel.Writer.TryWrite(message);    
    }
}