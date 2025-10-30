using System.Drawing;
using webapi.Controllers.Models;
using webapi.Infrastructure.Repositories;
using webapi.Utilities;

namespace webapi.Services.PixelService;

public class PixelService
{
    private readonly DelayedQueue<SetPixelTask> _delayedTasks = new(50);
    private readonly Broadcast _messenger;
    private readonly PixelRepository _pixelRepository;
    

    public PixelService(
        PixelRepository pixelRepository,
        Broadcast messenger
    )
    {
        _pixelRepository = pixelRepository;
        _messenger = messenger;
        
        Init();
    }

    private async Task Init()
    {
        await foreach (var queue in _delayedTasks.GetStream())
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

            await _messenger.SendPixelSettingListMessage(messages);

            foreach (var source in taskSources)
                source.SetResult();
        }
    }

    public Task SetPixel(int x, int y, Color color)
    {
        ValidatePixel(x, y);
        var delayedTask = new SetPixelTask(x, y, color);
        
        _delayedTasks.Enqueue(delayedTask);
        return delayedTask.Tcs.Task;
    }

    public void ValidatePixel(int x, int y)
    {
        var (width, height) = GetSizes();
        if (x >= width || y >= height) throw new GameException();
    }
    
    
    

    public (int, int) GetSizes() => _pixelRepository.GetSizes();

    public byte[] GetBitmap() => _pixelRepository.GetBitmap();
}