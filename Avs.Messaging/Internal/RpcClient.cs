using Avs.Messaging.Contracts;
using Avs.Messaging.Core;

namespace Avs.Messaging.Internal;

internal class RpcClient(IMessageTransport transport, MessagingOptions options) : IRpcClient
{
    public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(options.RequestReplyTimeout);

        try
        {
            return await transport.RequestAsync<TRequest, TResponse>(request, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new RequestReplyException(RequestReplyError.Cancelled, "Request cancelled");
        }
    }
}