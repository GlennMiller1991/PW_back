using System.Net.WebSockets;

namespace webapi.Services;

public class Player
{
    public int Id { get; init; }
    public WebSocket Socket { get; private set; }
    public GameRole Role { get; private set; } = GameRole.Challenger;

    public Task UpgradeRole()
    {
        if (Role == GameRole.Player) return Task.CompletedTask;
        Role = GameRole.Player;
        return Broadcast.SendStatusChangeMessage(Socket);
    }

    public Player(int id, WebSocket socket)
    {
        Id = id;
        Socket = socket;
        SetSocket(socket);
    }
    public DateTime ConnectionBirthTime { get; private set; }

    public DateTime LastActionTime { get; set; } 

    public TaskCompletionSource Tcs { get; set; } = new();

    public Task Completion => Tcs.Task;

    public Task Finish()
    {
        Tcs.SetResult();
        return CloseSocket();
    }

    public bool IsConnectionAlive() => IsConnectionAlive(Socket);

    public Task ReplaceSocketAsync(WebSocket socket)
    {
        var finishCompletion = Finish();
        SetSocket(socket);
        return finishCompletion;
    }

    private void SetSocket(WebSocket socket)
    {
        Socket = socket;
        Tcs = new TaskCompletionSource();
        ConnectionBirthTime = DateTime.Now;
        LastActionTime = ConnectionBirthTime - TimeSpan.FromMinutes(2);
    }

    public Task CloseSocket() => 
        Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
        .ContinueWith(_ => Socket.Dispose());
    public static bool IsConnectionAlive(WebSocket socket) => socket.State == WebSocketState.Open;

    public static bool IsActive(Player player) => player.Role == GameRole.Player;
    public static bool IsNotActive(Player player) => !IsActive(player);
}