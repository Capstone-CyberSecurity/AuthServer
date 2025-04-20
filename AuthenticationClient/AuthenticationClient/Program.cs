using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private TcpClient server;
    private NetworkStream stream;
    private Crypto crypto = new Crypto();

    public async Task Run(string username)
    {
        server = new TcpClient();

        try
        {
            await server.ConnectAsync("127.0.0.1", 39990);
            stream = server.GetStream();
            Console.WriteLine("Connected to server");

            Packet loginPacket = new Packet { packetType = PacketType.LOGIN, data = Encoding.UTF8.GetBytes(username) };
            
            // 로그인 메시지 전송
            await SendBytesAsync(loginPacket.ToBytes());

        RCV:
            // 응답 확인
            byte[] response = await ReceiveBytesAsync();
            Packet rcvPacket = Packet.FromBytes(response);
            Console.WriteLine($"서버 응답: {rcvPacket.packetType}");

            if (rcvPacket.packetType == PacketType.LOGIN_OK)
            {
                byte[] pubKey = rcvPacket.data;
                crypto.LoadPublicKey(pubKey);
                //AES 키 생성
                byte[] aesKey = Crypto.GenerateRandomKey();
                crypto.SetAesGcmKey(aesKey);
                byte[] aesIV = Crypto.GenerateRandomIV();
                //키 암호화
                byte[] encryptedKey = crypto.RsaEncrypt(aesKey);
                
                Packet sendPacket = new Packet { packetType = PacketType.KEY, data = encryptedKey };
                await SendBytesAsync(sendPacket.ToBytes());
                Console.WriteLine($"키 전송: {Encoding.UTF8.GetString(encryptedKey)}");
                goto RCV;
            }
            else if (rcvPacket.packetType == PacketType.CONNECT)
            {
                // 서버 메시지 루프 시작
                await ListenToServer();
            }
            else
            {
                Console.WriteLine("로그인 실패");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task SendBytesAsync(byte[] message)
    {
        var lengthPrefix = BitConverter.GetBytes(message.Length); // 4바이트 길이
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix); // 네트워크 바이트 오더로 (big endian)

        await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length); // 길이 먼저 보내기
        await stream.WriteAsync(message, 0, message.Length);           // 실제 데이터 전송
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

        return messageBuffer;
    }

    private async Task ReadExactAsync(byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int bytesRead = await stream.ReadAsync(buffer, offset, count);
            if (bytesRead == 0)
                throw new IOException("Disconnected");

            offset += bytesRead;
            count -= bytesRead;
        }
    }

    private async Task ListenToServer()
    {
        while (server.Connected)
        {
            try
            {
                byte[] response = await ReceiveBytesAsync();
                Packet rcvPacket = Packet.FromBytes(response);
                Console.WriteLine($"서버에서 수신: {rcvPacket.packetType} / {rcvPacket.IV}:{rcvPacket.IV.Length}, {rcvPacket.tag}:{rcvPacket.tag.Length}");

                //복호화 진행
                byte[] decMessage = crypto.AesGcmDecrypt(rcvPacket.IV, rcvPacket.data, rcvPacket.tag);
                Console.WriteLine($"복호화 테스트: {Encoding.UTF8.GetString(decMessage)}");

                if (rcvPacket.packetType == PacketType.HEART)
                {
                    //패킷 직렬화 및 암호화
                    byte[] aesIV = Crypto.GenerateRandomIV();
                    (byte[] message, byte[] tag) = crypto.AesGcmEncrypt(aesIV, new byte[1] { 0x10 });

                    Packet sendPacket = new Packet { packetType = PacketType.BEAT, IV = aesIV, tag = tag, data = message };

                    await SendBytesAsync(sendPacket.ToBytes());
                    Console.WriteLine("Heart Beat 응답 전송");
                }
                // 추가 명령들 여기서 처리 가능
            }
            catch(Exception e)
            {
                Console.WriteLine($"서버와 연결 끊김 {e.Message}");
                break;
            }
        }
    }
    
    public static async Task Main(string[] args)
    {
        Console.Write("유저 이름 입력: ");
        string username = Console.ReadLine();

        Program client = new Program();
        await client.Run(username);
    }
}

public class Crypto
{
    private RSA rsa;
    private byte[] aesKey;

    public Crypto()
    {
        //rsa private 키 생성
        rsa = RSA.Create(2048);
    }

    //RSA 관련
    public byte[] GetPublicKeyBytes()
    {
        return rsa.ExportRSAPublicKey();
    }

    public void LoadPublicKey(byte[] publicKeyBytes)
    {
        rsa.ImportRSAPublicKey(publicKeyBytes, out _);
    }

    public byte[] RsaEncrypt(byte[] data)
    {
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] RsaDecrypt(byte[] encryptedData)
    {
        return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
    }

    // AES-GCM 관련
    public void SetAesGcmKey(byte[] key)
    {
        aesKey = key;
    }

    public (byte[] ciphertext, byte[] tag) AesGcmEncrypt(byte[] iv, byte[] plaintext)
    {
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using var aesGcm = new AesGcm(aesKey);
        aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
        return (ciphertext, tag);
    }

    public byte[] AesGcmDecrypt(byte[] iv, byte[] ciphertext, byte[] tag)
    {
        byte[] plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(aesKey);
        aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
        return plaintext;
    }

    // AES-GCM 키, IV 생성
    public static byte[] GenerateRandomKey(int size = 32) => RandomNumberGenerator.GetBytes(size);
    public static byte[] GenerateRandomIV(int size = 12) => RandomNumberGenerator.GetBytes(size);
}