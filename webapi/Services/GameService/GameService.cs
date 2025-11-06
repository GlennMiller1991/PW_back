using System.Drawing;
using System.Net.WebSockets;
using webapi.Infrastructure.Repositories;
using webapi.Utilities;

namespace webapi.Services.GameService;

public class GameService
{
    private readonly DelayedQueue<PixelInfo> _paintingQueue = new(50);
    private readonly PixelRepository _pixelRepository;
    private readonly TimeSpan _reloadTimeout = TimeSpan.FromMilliseconds(1000);
    public ActivePlayers ActivePlayers { get; } = new();
    private readonly ActivePlayersActualizer _actualizer;

    private readonly Lock _savedStateLock = new();
    private byte[] SavedVersionedBitmap { get; set; }
    private int SavedBitmapVersion { get; set; }

    public (byte[] bitmap, int version) GetSavedState()
    {
        lock (_savedStateLock)
            return (SavedVersionedBitmap, SavedBitmapVersion);
    }

    public GameService(PixelRepository pixelRepository)
    {
        _pixelRepository = pixelRepository;
        _actualizer = new(ActivePlayers);

        SavedVersionedBitmap = pixelRepository.GetBitmapCopy();
        SavedBitmapVersion = 0;

        _ = Init();
    }

    private async Task Init()
    {
        await foreach (var queue in _paintingQueue.GetStream())
        {
            try 
            {
                var messages = new (int x, int y, Color color, int version)[queue.Count];
                var i = 0;
                foreach (var task in queue)
                {
                    _pixelRepository.SetPixel(task);
                    messages[i++] = (task.X, task.Y, task.Color, SavedBitmapVersion + i);
                }

                var connections = ActivePlayers
                    .GetAllPlayers()
                    .Select(p => p.Socket);

                var newVersion = SavedBitmapVersion + i;
                var copyTask = Task.Run(() =>
                {
                    var bitmap = new byte[SavedVersionedBitmap.Length + 4];
                    int[] intArr = [newVersion];
                    Buffer.BlockCopy(intArr, 0, bitmap, 0, 4);
                    _pixelRepository.GetBitmapCopy(bitmap, 4);
                    return bitmap;
                });
                var failedConnections = Broadcast.SendPixelSettingListMessage(messages, connections);
                await Task.WhenAll(copyTask, failedConnections);

                lock (_savedStateLock)
                {
                    SavedVersionedBitmap = copyTask.Result;
                    SavedBitmapVersion += i;
                }
            } catch (Exception e) {
                Console.WriteLine("Bug");
            }
            
        }
    }

    public Player AddPlayer(WebSocket socket, int userId)
    {
        var player = ActivePlayers.AddPlayer(socket, userId);

        Task.Run(_actualizer.ActualizePlayers);
        return player;
    }

    public void SetPixel(Player player, int x, int y, Color color)
    {
        ValidatePixel(x, y);

        lock (player.PlayerLock)
        {
            ValidatePlayer(player);
            player.LastActionTime = DateTime.Now;
        }

        var paintingTask = new PixelInfo(x, y, color);

        _paintingQueue.Enqueue(paintingTask);
    }

    public void ValidatePixel(int x, int y)
    {
        var (width, height) = GetSizes();
        if (x >= width || y >= height)
            throw new GameException();
    }

    public void ValidatePlayer(Player player)
    {
        var now = DateTime.Now;
        if (now - player.LastActionTime < _reloadTimeout)
            throw new GameException();
    }


    public (int, int) GetSizes() => _pixelRepository.GetSizes();

    public byte[] GetBitmap() => _pixelRepository.GetBitmap();
}