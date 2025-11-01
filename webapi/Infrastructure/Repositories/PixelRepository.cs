using System.Drawing;
using webapi.Utilities;

namespace webapi.Infrastructure.Repositories;

public class PixelRepository
{
    private readonly int _width = 100;
    private readonly int _height = 100;
    private byte[] _bitmap;
    private AsyncQueue _queue = new();

    public PixelRepository()
    {
        CreateBitmap();
        ClearBitmap();
    }

    private void CreateBitmap()
    {
        _bitmap = new byte[_width * _height * 3];
    }

    private void ClearBitmap()
    {
        Array.Clear(_bitmap);
    }

    public Task SetPixelAsync(int x, int y, Color color)
    {
        var r = y * _width * 3 + x * 3;
        return _queue.AddWork(() =>
        {
            _bitmap[r] = color.R;
            _bitmap[r + 1] = color.G;
            _bitmap[r + 2] = color.B;
        });
    }

    public void SetPixel(PaintingTask pixelInfo)
    {
        var r = pixelInfo.Y * _width * 3 + pixelInfo.X * 3;
        var color = pixelInfo.Color;
        _bitmap[r] = color.R;
        _bitmap[r + 1] = color.G;
        _bitmap[r + 2] = color.B;
    }

    public (int, int) GetSizes() => (_width, _height);

    public byte[] GetBitmap() => _bitmap;
}