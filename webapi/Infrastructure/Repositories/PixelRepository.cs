using System.Drawing;
using webapi.Services.GameInfra;

namespace webapi.Infrastructure.Repositories;

public class PixelRepository
{
    private readonly int _width = 1000;
    private readonly int _height = 1000;
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

    public void ClearBitmap(byte[]? with = null)
    {
        if (with == null) Array.Clear(_bitmap);
        else
        {
            if (with.Length == _width * _height * 3)
                _bitmap = with;
        }
    }

    public void SetPixel(SetPixelCommand setPixelCommand)
    {
        var r = (setPixelCommand.Y * _height + setPixelCommand.X) * 3;
        var color = setPixelCommand.Color;
        _bitmap[r] = color.R;
        _bitmap[r + 1] = color.G;
        _bitmap[r + 2] = color.B;
    }

    public (int width, int height) GetSizes() => (_width, _height);

    public byte[] GetBitmap() => _bitmap;

    public Color GetColorAtPosition(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) throw new Exception();
        x = (y + x) * 3;
        return Color.FromArgb(_bitmap[x], _bitmap[x + 1], _bitmap[x + 2]);
    }

    public byte[] GetBitmapCopy(byte[]? dst = null, int dstOffset = 0)
    {
        dst ??= new byte[_bitmap.Length];
        var src = _bitmap;
        Buffer.BlockCopy(_bitmap, 0, dst, dstOffset, src.Length);

        return dst;
    }
}