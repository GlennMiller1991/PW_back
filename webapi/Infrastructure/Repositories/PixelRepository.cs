using System.Drawing;

namespace webapi.Infrastructure.Repositories;

public class PixelRepository
{
    private readonly int _width = 100;
    private readonly int _height = 100;
    private byte[] _bitmap;

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

    public void SetPixel(int x, int y, Color color)
    {
        var r = y * _width * 3 + x * 3;
        _bitmap[r] = color.R;
        _bitmap[r + 1] = color.G;
        _bitmap[r + 2] = color.B;
    }

    public (int, int) GetSizes() => (_width, _height);

    public byte[] GetBitmap() => _bitmap;
}