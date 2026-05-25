using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Turning.Infrastructure.WebSockets;

public sealed class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public bool TryAdd(string sessionId, WebSocket socket)
    {
        return _connections.TryAdd(sessionId, socket);
    }

    public bool TryRemove(string sessionId, out WebSocket? socket)
    {
        return _connections.TryRemove(sessionId, out socket);
    }

    public bool TryGet(string sessionId, out WebSocket? socket)
    {
        return _connections.TryGetValue(sessionId, out socket);
    }
}
