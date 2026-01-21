using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using webapi.Services.GameInfra;

namespace webapi.Services;

public class ImageMaker(GameService gameService)
{

    public MemoryStream GetImage()
    {
        var (bitmap, _) = gameService.GetSavedState();
        return ByteArrToBmp(bitmap);
    }
    
    public static MemoryStream ByteArrToBmp(byte[] arr)
    {
        var len = arr.Length - 6;
        var size = Convert.ToInt32(Math.Sqrt(len / 3));

        using var image = new Image<Rgb24>(size, size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixelIndex = (y * size + x) * 3;
                var r = arr[6 + pixelIndex];
                var g = arr[6 + pixelIndex + 1];
                var b = arr[6 + pixelIndex + 2];
                image[x, y] = new Rgb24(r, g, b);
            }
        }

        var stream = new MemoryStream();
        image.Save(stream, new BmpEncoder());
        stream.Position = 0;

        return stream;
    }

    public static async Task<byte[]> BmpToByteArr(MemoryStream stream, (int width, int height)? sizes = null)
    {
        stream.Position = 0;
        byte[] bitmap;
        using (var img = await Image.LoadAsync(stream))
        {
            if (sizes != null)
            {
                img.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(sizes.Value.width, sizes.Value.height),
                    Mode = ResizeMode.Stretch,
                }));        
            }
        

            using (var rgbImage = img.CloneAs<Rgb24>())
            {
                bitmap = new byte[rgbImage.Width * rgbImage.Height * 3];
                rgbImage.CopyPixelDataTo(bitmap);
            }
        }

        return bitmap;
    }
}