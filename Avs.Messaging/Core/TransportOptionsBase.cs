namespace Avs.Messaging.Core;

public abstract class TransportOptionsBase
{
    private readonly List<Type> _consumerTypes = new();
    public bool UseRpcClient { get; private set; }
    
    public void AddConsumer<T>()
    {
        _consumerTypes.Add(typeof(T));
    }

    public void AddRpcClient()
    {
        UseRpcClient = true;
    }
    
    /// <summary>
    /// Consumers restricted to the given transport type
    /// </summary>
    public List<Type> Consumers => _consumerTypes;
}