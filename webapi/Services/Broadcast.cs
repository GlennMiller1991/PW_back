using System.Drawing;
using System.Net.WebSockets;
using webapi.Utilities;

namespace webapi.Services;

internal enum Room : byte
{
    Game = 1,
}

internal enum GameMessageType : byte
{
    StatusChange = 1,
    PixelSetting  = 2,
}

public class Broadcast(WsConnectionManager connectionManager)
{
    public Task SendMessage(byte[] msg, int? userId = null)
    {
        var tasks = new List<Task>();
        var abort = CancellationToken.None;
        if (userId == null)
            foreach (var socket in connectionManager.GetAllAliveSockets())
                tasks.Add(socket.SendAsync(msg, WebSocketMessageType.Binary, true, abort));
        else
        {
            var connection = connectionManager.GetByUserId((int)userId!);
            if (connection != null)
                tasks.Add(connection.SendAsync(msg, WebSocketMessageType.Binary, true, abort));
        }

        return Task.WhenAll(tasks);
    }
    
    public Task SendStatusChangeMessage(int userId)
    {
        var byteArray = new[]{(byte)Room.Game, (byte)GameMessageType.StatusChange};
        return SendMessage(byteArray, userId);
    }
    public Task SendPixelSettingMessage(int x, int y, int r, int g, int b)
    {        
        var intArray = new []{x, y, r, g, b};
        var byteArray = new byte[intArray.Length * sizeof(int) + 2];
        byteArray[0] = (byte)Room.Game;
        byteArray[1] = (byte)GameMessageType.PixelSetting;
        Buffer.BlockCopy(intArray, 0, byteArray, 2 ,byteArray.Length - 2);

        return SendMessage(byteArray);
    }

    public Task SendPixelSettingListMessage((int x, int y, Color color)[] arr)
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

        return SendMessage(byteArray);
    }
    
}