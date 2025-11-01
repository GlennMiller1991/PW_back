using System.Drawing;
using System.Net.WebSockets;

namespace webapi.Services;

internal enum Room : byte
{
    Game = 1,
}

internal enum GameMessageType : byte
{
    StatusChange = 1,
    PixelSetting = 2,
}

public abstract class Broadcast
{
    public static Task SendPixelSettingListMessage((int x, int y, Color color)[] arr, WebSocket[] connections)
    {
        var intArray = new int[arr.Length * 5];
        var i = 0;
        foreach (var (x, y, color) in arr)
        {
            intArray[i] = x;
            intArray[i + 1] = y;
            intArray[i + 2] = color.R;
            intArray[i + 3] = color.G;
            intArray[i + 4] = color.B;
            i += 5;
        }

        var byteArray = new byte[intArray.Length * sizeof(int) + 2];
        byteArray[0] = (byte)Room.Game;
        byteArray[1] = (byte)GameMessageType.PixelSetting;
        Buffer.BlockCopy(intArray, 0, byteArray, 2, byteArray.Length - 2);

        return SendMessage(byteArray, connections);
    }

    public static Task SendStatusChangeMessage(WebSocket socket)
    {
        var byteArray = new[] { (byte)Room.Game, (byte)GameMessageType.StatusChange };
        return SendMessage(byteArray, [socket]);
    }

    public static Task SendMessage(byte[] msg, WebSocket[] connections)
    {
        var tasks = new List<Task>();
        var abort = CancellationToken.None;
        foreach (var connection in connections)
            tasks.Add(connection.SendAsync(msg, WebSocketMessageType.Binary, true, abort));

        return Task.WhenAll(tasks);
    }
}