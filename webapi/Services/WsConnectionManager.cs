using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace webapi.Services;

public class WsConnectionManager
{
    private readonly ConcurrentDictionary<string, (
        WebSocket socket, 
        Task taskCompletion,
        DateTime timestamp,
        GameRole role
        )> _sockets = new();
    
    public Task AddSocket(WebSocket socket, int userId)
    {
        var connectionId = userId.ToString();
        
        var task = new Task(() => { });

        if (!IsWsOpen(socket))
            return Task.CompletedTask;

        if (!_sockets.TryAdd(connectionId, (socket, task, DateTime.Now, GameRole.Challenger)))
            throw new Exception("Bad request");

        return task;
    }

    public async Task RemoveSocket(string connectionId)
    {
        var res = _sockets.TryRemove(connectionId, out var entry);
        if (!res) return;
        await entry.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        entry.socket.Dispose();
        entry.taskCompletion.Start();
    }

    public Task RemoveSocket(int userId) => RemoveSocket(userId.ToString());

    public Task RemoveSocket(WebSocket socket)
    {
        KeyValuePair<string, (WebSocket, Task, DateTime, GameRole)> entry = _sockets.FirstOrDefault((s) => s.Value.socket == socket);
        return RemoveSocket(entry.Key);
    }

    public WebSocket? GetByUserId(int userId)
    {
        var res = _sockets.TryGetValue(userId.ToString(), out var entry);
        if (!res) return null;
        return entry.socket;
    }

    public (WebSocket socket, Task task, DateTime timestamp, GameRole role)[] GetAllSockets()
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

    public static bool IsWsOpen(WebSocket ws)
    {
        return ws.State == WebSocketState.Open;
    }
}

public enum GameRole
{
    Challenger = 1,
    Player = 2,
}