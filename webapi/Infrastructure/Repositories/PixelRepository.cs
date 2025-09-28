using System.Drawing;

namespace webapi.Infrastructure.Repositories;

public class PixelRepository
{
    private int _width = 10;
    private int _height = 10;
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
        for (var i = 0; i < _bitmap.Length; i++)
            _bitmap[i] = 0x0;
    }

    public void SetPixel(int x, int y, Color color)
    {
        var r = y * _width * 3 + x * 3;
        _bitmap[r] = color.R;
        _bitmap[r + 1] = color.G;
        _bitmap[r + 2] = color.B;
    }
}