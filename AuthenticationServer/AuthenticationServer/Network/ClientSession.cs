using System.Text;
using AuthenticationServer.Utility;

namespace AuthenticationServer.Network;

public class ClientSession
{
    public TcpClient Client { get; private set; }
    public Crypto crypto = new Crypto();
    public NetworkStream Stream => Client.GetStream();
    public string Id { get; set; } // com인지 bec인지 판단
    public string NIC { get; set; }
    public string UID { get; set; }

    public ClientSession(TcpClient client)
    {
        this.Client = client;
    }

    public async Task SendBytesAsync(byte[] message)
    {
        var lengthPrefix = BitConverter.GetBytes(message.Length); // 4바이트 길이
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix); // 네트워크 바이트 오더로 (big endian)

        //전송할 패킷 출력
        //Console.WriteLine($"전송할 HEX: {BitConverter.ToString(message)}");

        await Stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length); // 길이 먼저 보내기
        await Stream.WriteAsync(message, 0, message.Length);           // 실제 데이터 전송
    }

    public async Task<byte[]> ReceiveBytesAsync()
    {
        var lengthBuffer = new byte[4];
        await ReadExactAsync(lengthBuffer, 0, 4); // 길이 먼저 읽기

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBuffer);
        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        var messageBuffer = new byte[messageLength];
        await ReadExactAsync(messageBuffer, 0, messageLength); // 전체 메시지 읽기

        //받은 패킷 출력
        //Console.WriteLine($"받은 HEX: {BitConverter.ToString(messageBuffer)}");

        return messageBuffer;
    }

    private async Task ReadExactAsync(byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int bytesRead = await Stream.ReadAsync(buffer, offset, count);
            if (bytesRead == 0)
                throw new IOException("Disconnected");

            offset += bytesRead;
            count -= bytesRead;
        }
    }

    public async Task<byte[]> ReceiveBytesAsync(CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];

        // 1. 길이 먼저 읽기
        await ReadExactAsync(lengthBuffer, 0, 4, cancellationToken);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBuffer);

        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        // 2. 본문 읽기
        var messageBuffer = new byte[messageLength];
        await ReadExactAsync(messageBuffer, 0, messageLength, cancellationToken);

        return messageBuffer;
    }

    private async Task ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        while (count > 0)
        {
            int bytesRead = await Stream.ReadAsync(buffer, offset, count, cancellationToken);
            if (bytesRead == 0)
                throw new IOException("Disconnected");

            offset += bytesRead;
            count -= bytesRead;
        }
    }
}