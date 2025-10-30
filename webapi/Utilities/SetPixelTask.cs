using System.Drawing;

namespace webapi.Utilities;

public class SetPixelTask(int x, int y, Color color)
{
    public TaskCompletionSource Tcs { get; } = new();

    public int X => x;
    public int Y => y;
    public Color Color => color;

}