using System.Drawing;

namespace webapi.Services.GameService;

public class PixelInfo(int x, int y, Color color)
{
    public int X => x;
    public int Y => y;
    public Color Color => color;

}