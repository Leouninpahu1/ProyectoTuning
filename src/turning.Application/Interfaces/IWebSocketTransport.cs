namespace Turning.Application.Interfaces;

public interface IWebSocketTransport
{
    /// <summary>
    /// Sends a payload (will be serialized) to the client identified by sessionId.
    /// </summary>
    Task SendAsync(string sessionId, object payload, CancellationToken cancellationToken = default);
}
