using System.Collections.Concurrent;
using System.Threading.Channels;
using Contracts;

namespace MessageConsumer.Services;

/// <summary>
/// Singleton instance which manages connected channels
/// </summary>
public class ChannelManager(ILogger<ChannelManager> logger)
{
    private readonly ConcurrentDictionary<Guid, Channel<NewUser>> _channels = new();

    /// <summary>
    /// Gets a channel using Channel ID
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <returns>A channel instance</returns>
    public Channel<NewUser> GetChannel(Guid channelId)
    {
        Channel<NewUser> channel = _channels.GetOrAdd(channelId,
            (_) => Channel.CreateBounded<NewUser>(
                new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest }));
        
        return channel;
    }

    /// <summary>
    /// Attempts to mark a channel as completed and remove from memory cache
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    public void CloseChannel(Guid channelId)
    {
        if (!_channels.TryRemove(channelId, out var channel))
        {
            return;
        }

        channel.Writer.TryComplete();
        _channels.TryRemove(channelId, out _);
        logger.LogInformation($"Channel {channelId} has been closed.");
    }
    
    /// <summary>
    /// Active channels
    /// </summary>
    public IEnumerable<Channel<NewUser>> Channels => _channels.Values;
}