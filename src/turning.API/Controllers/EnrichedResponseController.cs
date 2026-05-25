using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using Turning.Application.Features.EnrichedResponse;
using Turning.Application.Interfaces;
using Turning.Infrastructure.WebSockets;

namespace Turning.API.Controllers;

[ApiController]
[Route("ws/enriched-response")]
public class EnrichedResponseController : ControllerBase
{
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly IWebSocketTransport _webSocketTransport;
    private readonly IEnrichedResponseFactory _factory;

    public EnrichedResponseController(WebSocketConnectionManager connectionManager, IWebSocketTransport webSocketTransport, IEnrichedResponseFactory factory)
    {
        _connectionManager = connectionManager;
        _webSocketTransport = webSocketTransport;
        _factory = factory;
    }

    [HttpGet("{sessionId:guid}")]
    public async Task Get(Guid sessionId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var id = sessionId.ToString();
        _connectionManager.TryAdd(id, socket);

        // Send an initial enriched payload as a demo
        var demo = _factory.Create(sessionId, "Hello from server", "neutral", 0.95, "ai", "text_response");
        await _webSocketTransport.SendAsync(id, demo);

        var buffer = new byte[1024 * 4];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                // ignore incoming messages for now
            }
        }
        finally
        {
            _connectionManager.TryRemove(id, out _);
            if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}
