using System.Net.WebSockets;
using System.Text;

namespace webapi.Services;

public class Broadcast(WsConnectionManager connectionManager)
{
    public async Task BroadcastPixel(int x, int y, int r, int g, int b)
    {
        var tasks = new List<Task>();
        var intArray = new []{x, y, r, g, b};
        var byteArray = new byte[intArray.Length * sizeof(int)];
        Buffer.BlockCopy(intArray, 0, byteArray, 0 ,byteArray.Length);
        
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