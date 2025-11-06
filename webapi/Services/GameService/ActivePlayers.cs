using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace webapi.Services.GameService;

public delegate T PlayerProcessor<T>(IEnumerable<Player> players);

public delegate void PlayerProcessor(IEnumerable<Player> players);

public delegate void LockedWork();

public delegate T LockedWork<T>();

public class ActivePlayers
{
    private readonly ConcurrentDictionary<int, Player> _players = new();
    private readonly Lock _playerConsistency = new();

    public Player AddPlayer(WebSocket socket, int userId)
    {
        return WorkUnderLock(() =>
        {
            var player = GetByUserId(userId);
            if (player != null)
                _ = player.ReplaceSocketAsync(socket);
            else
            {
                player = new Player(userId, socket);
                if (!_players.TryAdd(player.Id, player))
                    _ = player.Finish();
            }

            return player;
        });
    }

    public ValueTask RemovePlayer(int userId, bool withLock = true)
    {
        return withLock ? WorkUnderLock(Fn) : Fn();

        ValueTask Fn()
        {
            var res = _players.TryRemove(userId, out var player);
            return res ? new ValueTask(player!.Finish()) : new ValueTask();
        }
    }

    public Player? GetByUserId(int userId)
    {
        _players.TryGetValue(userId, out var player);
        return player;
    }


    public IEnumerable<Player> GetAllPlayers()
    {
        return _players.Select((entry) => entry.Value);
    }

    public void ProcessPlayers(PlayerProcessor processor)
    {
        ProcessPlayers((players) =>
        {
            processor(players);
            return true;
        });
    }

    public T ProcessPlayers<T>(PlayerProcessor<T> processor) =>
        WorkUnderLock(() => processor(GetAllPlayers()));

    public void WorkUnderLock(LockedWork work)
    {
        WorkUnderLock(() =>
        {
            work();
            return true;
        });
    }

    public T WorkUnderLock<T>(LockedWork<T> work)
    {
        lock (_playerConsistency)
            return work();
    }
}

public enum GameRole
{
    Challenger = 1,
    Player = 2,
}