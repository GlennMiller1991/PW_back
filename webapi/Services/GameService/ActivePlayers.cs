using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace webapi.Services;

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
        Task? finishCompletion;
        Player? player;
        lock (_playerConsistency)
        {
            player = GetByUserId(userId);
            if (player != null)
                finishCompletion = player.ReplaceSocketAsync(socket);
            else
            {
                player = new Player(userId, socket);
                if (!_players.TryAdd(player.Id, player))
                    finishCompletion = player.Finish();
            }
        }

        return player;
    }

    public Task RemovePlayerLocked(int userId)
    {
        lock (_playerConsistency)
        {
            return RemovePlayerUnlocked(userId);
        }
    }

    public Task RemovePlayerUnlocked(int userId)
    {
        var res = _players.TryRemove(userId, out var player);
        return res ? player!.Finish() : Task.CompletedTask;
    }

    public Player? GetByUserId(int userId)
    {
        lock (_playerConsistency)
        {
            _players.TryGetValue(userId, out var player);
            return player;
        }
    }


    public IEnumerable<Player> GetAllPlayers()
    {
        return _players
            .ToArray()
            .Select((entry) => entry.Value);
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