using System.Text;

namespace AuthenticationServer.Network;

public class ClientSession
{
    public TcpClient Client { get; private set; }
    public NetworkStream Stream => Client.GetStream();
    public string Id { get; set; }

    public ClientSession(TcpClient client)
    {
        this.Client = client;
    }

    public async Task SendAsync(string message)
    {
        Byte[] datagram = Encoding.UTF8.GetBytes(message);
        await Stream.WriteAsync(datagram, 0, datagram.Length);
    }

    public async Task<string> ReceiveAsync()
    {
        var buffer = new byte[1024];
        int count = await Stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, count);
    }
}