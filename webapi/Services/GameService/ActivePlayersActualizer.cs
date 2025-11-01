namespace webapi.Services;

public class ActivePlayersActualizer(ActivePlayers activePlayers)
{
    private int _maxActiveQty = 10;
    private Timer? _timer;

    public Task OnAddNewPlayer(Player player, Player[] players)
    {
        if (players.Count(p => p.Role == GameRole.Player) < _maxActiveQty)
            return player.UpgradeRole();

        return Task.CompletedTask;
    }

    private void PlayersActualizer(IEnumerable<Player> players)
    {
        DisposeTimer();
        
        players = players.OrderBy(p => p.LastActionTime).ToArray();
        var active = players.Where(Player.IsActive).ToArray();
        var passive = players.Where(Player.IsNotActive).ToArray();

        var activeQty = active.Length;

        var nextCheckDelay = TimeSpan.Zero;
        var twoMin = TimeSpan.FromMinutes(2);
        foreach (var player in active)
        {
            var dif = DateTime.Now - player.LastActionTime;
            if (dif >= twoMin)
            {
                activePlayers.RemovePlayerUnlocked(player.Id);
                activeQty--;
            }
            else
            {
                nextCheckDelay = -dif;
                break;
            };
        }

        var isFreeNow = _maxActiveQty - activeQty;
        if (isFreeNow > 0 && nextCheckDelay == TimeSpan.Zero && passive.Length > 0)
            nextCheckDelay = twoMin;

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