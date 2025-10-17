using System.Drawing;
using webapi.Infrastructure.Repositories;

namespace webapi.Services.PixelService;

public class PixelService(PixelRepository pixelRepository)
{
    private AsyncLock? _previousWriter;

    public void SetPixel(int x, int y, Color color)
    {
        var (width, height) = GetSizes();
        if (x >= width || y >= height) throw new GameException();
        
        pixelRepository.SetPixel(x, y, color);
    }

    public (int, int) GetSizes() => pixelRepository.GetSizes();

    public byte[] GetBitmap() => pixelRepository.GetBitmap();
}