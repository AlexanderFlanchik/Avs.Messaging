namespace Avs.Messaging.Contracts;

public interface IRpcClient
{
    /// <summary>
    /// Request-reply implementation
    /// </summary>
    /// <param name="request">A request message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TRequest">Type of request message</typeparam>
    /// <typeparam name="TResponse">Type of response</typeparam>
    /// <returns>A task which returns a response message when resolves</returns>
    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}