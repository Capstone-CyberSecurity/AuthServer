using System.Text;
using AuthenticationServer.Utility;

namespace AuthenticationServer.Network;

public class ClientSession
{
    public TcpClient Client { get; private set; }
    public Crypto crypto = new Crypto();
    public NetworkStream Stream => Client.GetStream();
    public string Id { get; set; }

    public ClientSession(TcpClient client)
    {
        this.Client = client;
    }

    public async Task SendBytesAsync(byte[] message)
    {
        var lengthPrefix = BitConverter.GetBytes(message.Length); // 4����Ʈ ����
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix); // ��Ʈ��ũ ����Ʈ ������ (big endian)

        await Stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length); // ���� ���� ������
        await Stream.WriteAsync(message, 0, message.Length);           // ���� ������ ����
    }

    public async Task<byte[]> ReceiveBytesAsync()
    {
        var lengthBuffer = new byte[4];
        await ReadExactAsync(lengthBuffer, 0, 4); // ���� ���� �б�

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBuffer);
        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        var messageBuffer = new byte[messageLength];
        await ReadExactAsync(messageBuffer, 0, messageLength); // ��ü �޽��� �б�

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

        // 1. ���� ���� �б�
        await ReadExactAsync(lengthBuffer, 0, 4, cancellationToken);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBuffer);

        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        // 2. ���� �б�
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