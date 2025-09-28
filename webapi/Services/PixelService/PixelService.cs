using System.Drawing;
using webapi.Infrastructure.Repositories;

namespace webapi.Services.PixelService;

public class PixelService(PixelRepository pixelRepository)
{
    private AsyncLock? _previousWriter;

    public async Task SetPixel(int x, int y, Color color)
    {
        var previousWriter = _previousWriter;
        AsyncLock? currentWriter = null;
        _previousWriter = null;

        if (previousWriter != null)
        {
            currentWriter = _previousWriter = new AsyncLock();
            await previousWriter.Task;
        }
        
        pixelRepository.SetPixel(x, y, color);
        currentWriter?.Complete();
    }
}