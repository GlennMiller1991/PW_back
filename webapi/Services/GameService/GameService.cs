using System.Drawing;
using System.Net.WebSockets;
using webapi.Infrastructure.Repositories;
using webapi.Utilities;

namespace webapi.Services.GameService;

public class GameService
{
    private readonly DelayedQueue<PaintingTask> _paintingQueue = new(50);
    private readonly PixelRepository _pixelRepository;
    private readonly TimeSpan _reloadMs = TimeSpan.FromMilliseconds(1000);
    public ActivePlayers ActivePlayers { get; } = new();
    private readonly ActivePlayersActualizer _actualizer;

    public GameService(PixelRepository pixelRepository)
    {
        _pixelRepository = pixelRepository;
        _actualizer = new(ActivePlayers);
        
        Init();
    }

    private async Task Init()
    {
        await foreach (var queue in _paintingQueue.GetStream())
        {
            var taskSources = new TaskCompletionSource[queue.Count];
            var messages = new (int x, int y, Color color)[queue.Count];
            var i = 0;
            foreach (var task in queue)
            {
                _pixelRepository.SetPixel(task);
                taskSources[i] = task.Tcs;
                messages[i++] = (task.X, task.Y, task.Color);
            }

            var connections = ActivePlayers
                .GetAllPlayers()
                .Select(p => p.Socket)
                .ToArray();

            await Broadcast.SendPixelSettingListMessage(messages, connections);

            foreach (var source in taskSources)
                source.SetResult();
        }
    }

    public Player AddPlayer(WebSocket socket, int userId)
    {
        var player = ActivePlayers.AddPlayer(socket, userId);
        
        Task.Run(_actualizer.ActualizePlayers);
        return player;
    }

    public Task SetPixel(Player player, int x, int y, Color color)
    {
        ValidatePixel(x, y);

        ActivePlayers.WorkUnderLock(() =>
        {
            ValidatePlayer(player);
            player.LastActionTime = DateTime.Now;
        });
        
        var paintingTask = new PaintingTask(x, y, color);
        
        _paintingQueue.Enqueue(paintingTask);
        return paintingTask.Tcs.Task;
    }

    public void ValidatePixel(int x, int y)
    {
        var (width, height) = GetSizes();
        if (x >= width || y >= height) throw new GameException();
    }

    public void ValidatePlayer(Player player)
    {
        var now = DateTime.Now;
        if (now - player.LastActionTime < _reloadMs)
            throw new GameException();
    }
    
    
    public (int, int) GetSizes() => _pixelRepository.GetSizes();

    public byte[] GetBitmap() => _pixelRepository.GetBitmap();
}