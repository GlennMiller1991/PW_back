using System.Net.WebSockets;
using System.Text;

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
    public async Task BroadcastRoom()
    {
        var tasks = new List<Task>();
        var byteArray = new []{(byte)Room.Game, (byte)GameMessageType.StatusChange};
        var abort = CancellationToken.None;
        foreach (var socket in connectionManager.GetAllAliveSockets())
        {
            tasks.Add(
                socket.SendAsync(byteArray, WebSocketMessageType.Binary, true, abort)
            );
        }

        await Task.WhenAll(tasks);
    }
    public async Task BroadcastPixel(int x, int y, int r, int g, int b)
    {        var tasks = new List<Task>();        var intArray = new []{x, y, r, g, b};
        var byteArray = new byte[intArray.Length * sizeof(int) + 2];
        byteArray[0] = (byte)Room.Game;
        byteArray[1] = (byte)GameMessageType.PixelSetting;
        Buffer.BlockCopy(intArray, 0, byteArray, 2 ,byteArray.Length - 2);
        
        var abort = CancellationToken.None;
        foreach (var socket in connectionManager.GetAllAliveSockets())
        {
            tasks.Add(
                socket.SendAsync(byteArray, WebSocketMessageType.Binary, true, abort)
                );
        }

        await Task.WhenAll(tasks);
    }

    private ArraySegment<byte> MessageToBytes(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        return new ArraySegment<byte>(bytes);
    }
    
}