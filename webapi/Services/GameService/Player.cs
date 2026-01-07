using System.Net.WebSockets;

namespace webapi.Services.GameService;

public class Player
{
    public int Id { get; init; }
    public WebSocket Socket { get; private set; }
    public GameRole Role { get; private set; } = GameRole.Challenger;

    public readonly Lock PlayerLock = new ();

    public Task UpgradeRole()
    {
        Role = GameRole.Player;
        return Broadcast.SendStatusChangeMessage(Socket);
    }

    public Player(int id, WebSocket socket)
    {
        Id = id;
        Socket = socket;
        SetSocket(socket);
    }
    public DateTime LastActionTime { get; set; } 

    public TaskCompletionSource Tcs { get; set; } = new();

    public Task Completion => Tcs.Task;

    public Task Finish(bool notifyLogout = false)
    {
        if (Convert.ToBoolean(notifyLogout))
            Broadcast.SendLogoutMessage(Socket);
        
        var tcs = Tcs;
        return CloseSocket()
            .ContinueWith(_ => tcs.SetResult());
    }

    public bool IsConnectionAlive() => IsConnectionAlive(Socket);

    public Task ReplaceSocketAsync(WebSocket socket)
    {
        var finishCompletion = Finish(true);
        SetSocket(socket);
        if (IsActive(this)) 
            UpgradeRole();
        
        return finishCompletion;
    }

    private void SetSocket(WebSocket socket)
    {
        Socket = socket;
        Tcs = new TaskCompletionSource();
        LastActionTime = DateTime.Now - TimeSpan.FromSeconds(1);
    }

    public Task CloseSocket()
    {
        var socket = Socket;
        return socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
            .ContinueWith(_ => socket.Dispose());
    }
       
    public static bool IsConnectionAlive(WebSocket socket) => socket.State == WebSocketState.Open;

    public static bool IsActive(Player player) => player.Role == GameRole.Player;
    public static bool IsNotActive(Player player) => !IsActive(player);
}