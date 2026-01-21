using System.Drawing;

namespace webapi.Services.GameInfra;

public class BitmapCommand;

public class SetPixelCommand(int x, int y, Color color) : BitmapCommand
{
    public int X => x;
    public int Y => y;
    public Color Color => color;

    public int Red => Color.R;
    public int Green => Color.G;
    public int Blue => Color.B;
}

public class ClearCommand(byte[]? bitmap = null) : BitmapCommand
{
    public byte[]? Bitmap { get; } = bitmap;
}