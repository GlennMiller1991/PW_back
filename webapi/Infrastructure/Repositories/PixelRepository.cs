using webapi.Services.GameService;

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

    public void SetPixel(PixelInfo pixelInfo)
    {
        var r = pixelInfo.Y * _width * 3 + pixelInfo.X * 3;
        var color = pixelInfo.Color;
        _bitmap[r] = color.R;
        _bitmap[r + 1] = color.G;
        _bitmap[r + 2] = color.B;
    }

    public (int width, int height) GetSizes() => (_width, _height);

    public byte[] GetBitmap() => _bitmap;

    public byte[] GetBitmapCopy(byte[]? dst = null, int dstOffset = 0)
    {
        dst ??= new byte[_bitmap.Length];
        var src = _bitmap;
        Buffer.BlockCopy(_bitmap, 0, dst, dstOffset, src.Length);

        return src;
    }
}