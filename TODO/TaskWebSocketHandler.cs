using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TODO;

public class TaskWebSocketHandler
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();

    public async Task HandleClientAsync(HttpContext context, WebSocket webSocket)
    {
        var socketId = Guid.NewGuid();
        _sockets[socketId] = webSocket;

        await SendAsync(webSocket, new
        {
            type = "connected",
            message = "WebSocket connection established."
        });

        var buffer = new byte[1024];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        finally
        {
            _sockets.TryRemove(socketId, out _);

            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
        }
    }

    public async Task BroadcastTaskChangedAsync(string action, TodoTask? task = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "task_changed",
            action,
            taskId = task?.Id,
            title = task?.Title
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);
        var disconnectedSockets = new List<Guid>();

        foreach (var socketEntry in _sockets)
        {
            var socket = socketEntry.Value;

            if (socket.State != WebSocketState.Open)
            {
                disconnectedSockets.Add(socketEntry.Key);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                disconnectedSockets.Add(socketEntry.Key);
            }
        }

        foreach (var socketId in disconnectedSockets)
        {
            _sockets.TryRemove(socketId, out _);
        }
    }

    private static async Task SendAsync(WebSocket socket, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
