using System.Drawing;
using System.Net.WebSockets;

namespace webapi.Services.GameInfra;

internal enum Room : byte
{
    Game = 1,
    App = 2,
}

internal enum GameMessageType : byte
{
    StatusChange = 1,
    PixelSetting = 2,
    BitmapSetting = 3,
    ClearBitmap = 4,
}

internal enum AppMessageType : byte
{
    Logout = 1,
}

public abstract class Broadcast
{
    public static byte[] MakeBitmapSettingsMessage(byte[] bitmap)
    {
        var byteArray = new byte[bitmap.Length + 2];
        byteArray[0] = (byte)Room.Game;
        byteArray[1] = (byte)GameMessageType.BitmapSetting;
        Buffer.BlockCopy(bitmap, 0, byteArray, 2, bitmap.Length);
        return byteArray;
    }
    
    public static Task<List<WebSocket>> SendPixelSettingListMessage((int x, int y, Color color, int version)[] arr, IEnumerable<WebSocket> connections)
    {
        var intArray = new int[arr.Length * 6];
        var i = 0;
        foreach (var (x, y, color, version) in arr)
        {
            intArray[i] = version;
            intArray[i + 1] = x;
            intArray[i + 2] = y;
            intArray[i + 3] = color.R;
            intArray[i + 4] = color.G;
            intArray[i + 5] = color.B;
            i += 6;
        }

        var byteArray = new byte[intArray.Length * sizeof(int) + 2];
        byteArray[0] = (byte)Room.Game;
        byteArray[1] = (byte)GameMessageType.PixelSetting;
        Buffer.BlockCopy(intArray, 0, byteArray, 2, byteArray.Length - 2);

        return SendBroadcastMessage(byteArray, connections);
    }

    public static Task<List<WebSocket>> SendClearBitmapMessage(IEnumerable<WebSocket> connections)
    {
        var byteArray = new byte[2];
        byteArray[0] = (byte)Room.Game;
        byteArray[1] = (byte)GameMessageType.ClearBitmap;

        return SendBroadcastMessage(byteArray, connections);
    }

    public static Task SendStatusChangeMessage(WebSocket socket)
    {
        var byteArray = new[] { (byte)Room.Game, (byte)GameMessageType.StatusChange };
        return SendBroadcastMessage(byteArray, [socket]);
    }

    public static async Task<List<WebSocket>> SendBroadcastMessage(byte[] msg, IEnumerable<WebSocket> connections)
    {
        var tasks = new List<Task>();
        var abort = CancellationToken.None;
        var failed = new List<WebSocket>();
        foreach (var connection in connections)
        {
            var localConnection = connection;
            tasks.Add(
                localConnection.SendAsync(msg, WebSocketMessageType.Binary, true, abort)
                    .ContinueWith((task) =>
                    {
                        if (task.IsFaulted) failed.Add(localConnection);
                    })
            );
        }

        await Task.WhenAll(tasks);
        return failed;
    }

    public static Task SendLogoutMessage(WebSocket connection)
    {
        byte[] msg = [(byte)Room.App, (byte)AppMessageType.Logout];
        return connection.SendAsync(msg, WebSocketMessageType.Binary, true, CancellationToken.None);
    }
}