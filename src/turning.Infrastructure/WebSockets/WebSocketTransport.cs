using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Turning.Application.Interfaces;

namespace Turning.Infrastructure.WebSockets;

public sealed class WebSocketTransport : IWebSocketTransport
{
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public WebSocketTransport(WebSocketConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task SendAsync(string sessionId, object payload, CancellationToken cancellationToken = default)
    {
        if (!_connectionManager.TryGet(sessionId, out var socket) || socket is null)
            return;

        if (socket.State != WebSocketState.Open)
            return;

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
    }
}
