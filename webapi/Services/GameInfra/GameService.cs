using System.Drawing;
using System.Net.WebSockets;
using webapi.Infrastructure.Repositories;
using webapi.Utilities;

namespace webapi.Services.GameInfra;

public class GameService
{
    private readonly DelayedQueue<BitmapCommand> _paintingQueue = new(50);
    private readonly PixelRepository _pixelRepository;
    private readonly TimeSpan _reloadTimeout = TimeSpan.FromMilliseconds(100);
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

        SavedVersionedBitmap = GetBitmapCopy(0);
        SavedBitmapVersion = 0;

        _ = Init();
    }

    private byte[] GetBitmapCopy(int newVersion)
    {
        var sizes = _pixelRepository.GetSizes();
        var versionInBytes = sizeof(int);
        var bitmap = new byte[sizes.width * sizes.height * 3 + versionInBytes];
        int[] intArr = [newVersion];
        Buffer.BlockCopy(intArr, 0, bitmap, 0, versionInBytes);
        bitmap = _pixelRepository.GetBitmapCopy(bitmap, 4);
        return Broadcast.MakeBitmapSettingsMessage(bitmap);
    }

    private async Task Init()
    {
        await foreach (var queue in _paintingQueue.GetStream())
        {
            try
            {
                var messages = new (int x, int y, Color color, int version)[queue.Count];
                var i = 0;
                foreach (var cmd in queue)
                {
                    var type = cmd.GetType();
                    if (type == typeof(SetPixelCommand))
                    {
                        var pixelInfo = (SetPixelCommand)cmd;
                        _pixelRepository.SetPixel(pixelInfo);
                        messages[i++] = (pixelInfo.X, pixelInfo.Y, pixelInfo.Color, SavedBitmapVersion + i);                        
                    }

                    if (type == typeof(ClearCommand))
                    {
                        _pixelRepository.ClearBitmap(((ClearCommand)cmd).Bitmap);
                        Array.Clear(messages);
                        var color = _pixelRepository.GetColorAtPosition(0, 0);

                        messages[0] = (0, 0, color, SavedBitmapVersion + 2);
                        i = 1;
                        break;
                    }
                }

                var connections = ActivePlayers
                    .GetAllPlayers()
                    .Select(p => p.Socket);

                var newVersion = SavedBitmapVersion + i;
                var copyTask = Task.Run(() => GetBitmapCopy(newVersion));

                var failedConnections = Broadcast.SendPixelSettingListMessage(messages, connections);
                await Task.WhenAll(copyTask, failedConnections);

                lock (_savedStateLock)
                {
                    SavedVersionedBitmap = copyTask.Result;
                    SavedBitmapVersion += i;
                }
            }
            catch (Exception e)
            {
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

    public async Task RemovePlayer(int userId)
    {
        await ActivePlayers.RemovePlayer(userId);

        Task.Run(_actualizer.ActualizePlayers);
    }

    public void SetPixel(Player player, int x, int y, Color color)
    {
        ValidatePixel(x, y);

        lock (player.PlayerLock)
        {
            ValidatePlayer(player);
            player.LastActionTime = DateTime.Now;
        }

        var paintingTask = new SetPixelCommand(x, y, color);

        _paintingQueue.Enqueue(paintingTask);
    }

    public void Clear(byte[]? with = null)
    {
        _paintingQueue.Enqueue(new ClearCommand(with));
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