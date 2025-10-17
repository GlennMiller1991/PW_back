using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace webapi.Services;

public class WsConnectionManager
{
    private readonly ConcurrentDictionary<string, (WebSocket socket, Task task)> _sockets = new();

    public Task AddSocket(WebSocket socket, int userId)
    {
        var connectionId = userId.ToString();
        var task = new Task(() => { });
        _sockets.TryAdd(connectionId, (socket, task));
        return task;
    }

    public async Task RemoveSocket(string connectionId)
    {
        var res = _sockets.TryRemove(connectionId, out var entry);
        if (!res || !Convert.ToBoolean(entry)) return;
        await entry.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        entry.socket.Dispose();
        entry.task.Start();
    }

    public Task RemoveSocket(int userId) => RemoveSocket(userId.ToString());

    public Task RemoveSocket(WebSocket socket)
    {
        KeyValuePair<string, (WebSocket, Task)> entry = _sockets.FirstOrDefault((s) => s.Value.socket == socket);
        return RemoveSocket(entry.Key);
    }


    public (WebSocket socket, Task task)[] GetAllSockets()
    {
        return _sockets
            .ToArray()
            .Select((entry) => entry.Value)
            .ToArray();
    }

    public WebSocket[] GetAllAliveSockets()
    {
        bool isAlive;
        return GetAllSockets()
            .Where(entry =>
            {
                isAlive = entry.socket.State == WebSocketState.Open;
                if (!isAlive) RemoveSocket(entry.socket);
                return isAlive;
            })
            .Select(entry => entry.socket)
            .ToArray();
    }
}