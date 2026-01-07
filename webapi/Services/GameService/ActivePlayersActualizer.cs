namespace webapi.Services.GameService;

public class ActivePlayersActualizer(ActivePlayers activePlayers)
{
    private int _maxActiveQty = 2;
    private readonly TimeSpan _afkTimeout = TimeSpan.FromMinutes(2);
    private Timer? _timer;

    private void PlayersActualizer(IEnumerable<Player> players)
    {
        DisposeTimer();
        
        players = players.OrderBy(p => p.LastActionTime).ToArray();
        var active = players.Where(Player.IsActive).ToArray();
        var passive = players.Where(Player.IsNotActive).ToArray();

        var activeQty = active.Length;

        var nextCheckDelay = TimeSpan.Zero;
        foreach (var player in active)
        {
            var dif = DateTime.Now - player.LastActionTime;
            if (dif >= _afkTimeout)
            {
                activePlayers.RemovePlayer(player.Id, false);
                activeQty--;
            }
            else
            {
                nextCheckDelay = _afkTimeout - dif;
                break;
            }
        }

        var isFreeNow = _maxActiveQty - activeQty;
        if (isFreeNow > 0 && nextCheckDelay == TimeSpan.Zero && passive.Length > 0)
            nextCheckDelay = _afkTimeout;

        foreach (var player in passive[..Math.Min(isFreeNow, passive.Length)])
            player.UpgradeRole();

        if (nextCheckDelay <= TimeSpan.Zero) return;
        
        CreteTimer((int)nextCheckDelay.TotalMilliseconds);
    }

    public void ActualizePlayers()
    {
        activePlayers.ProcessPlayers(PlayersActualizer);
    }

    private void DisposeTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CreteTimer(int ms)
    {
        var cb = new TimerCallback(_ => ActualizePlayers());
        _timer = new Timer(cb, null, ms, Timeout.Infinite);

    }
}