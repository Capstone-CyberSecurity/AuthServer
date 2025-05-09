using AuthenticationServer.Utility;

namespace AuthenticationServer.Network;
public class ListenServer(ServerManager serverManager) : IServer
{
    private ServerManager _serverManager = serverManager;
    private TcpListener _listener;
    public event Action<TcpClient> OnClientConnected;

    public void Init()
    {
        _listener = new TcpListener(IPAddress.Any, 39990);
        _listener.Start();
        Console.WriteLine($"Listening on port {_listener.LocalEndpoint}");
    }

    public async Task Update()
    {
        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            OnClientConnected?.Invoke(client);
        }
    }

    public async Task<bool> LoginSession(ClientSession clientSession)
    {
        Console.WriteLine("ListenServer.cs 로그인 관련 작업");
        try
        {
        RCV:
            byte[] message = await clientSession.ReceiveBytesAsync();
            Packet packetRcv = Packet.FromBytes(message);

            //수신 체크
            Console.WriteLine(clientSession.Client.Client.RemoteEndPoint + " [Login Session] " + packetRcv.packetType);

            if (packetRcv.packetType == PacketType.LOGIN)
            {
                //byte[] pubRsaKey = clientSession.crypto.GetPublicKeyBytes();
                clientSession.Id = Encoding.UTF8.GetString(packetRcv.data);

                //RSA 키 전달
                Packet packet = new Packet { packetType = PacketType.LOGIN_OK, data = clientSession.crypto.GetPublicKeyBytes() };
                await clientSession.SendBytesAsync(packet.ToBytes());
                Console.WriteLine(clientSession.Client.Client.RemoteEndPoint + " [Login Session] Accept Login called: " + clientSession.Id);
            }
            else if (packetRcv.packetType == PacketType.KEY)
            {
                //키 복호화 후 저장
                byte[] aesKey = clientSession.crypto.RsaDecrypt(packetRcv.data);
                clientSession.crypto.SetAesGcmKey(aesKey);

                //패킷 작성
                Packet packetSend = new Packet { packetType = PacketType.CONNECT, data = new byte[1] { 0x00 } };
                await clientSession.SendBytesAsync(packetSend.ToBytes());
                Console.WriteLine(clientSession.Id + " [Login Session] Get AES key");
            }
            else if(packetRcv.packetType == PacketType.NIC)
            {
                //패킷 복호화
                byte[] decrypt = clientSession.crypto.AesGcmDecrypt(packetRcv.IV, packetRcv.data, packetRcv.tag);

                string nicData = Encoding.UTF8.GetString(decrypt);
                clientSession.NIC = nicData;
                Console.WriteLine($"클라이언트 {clientSession.Id} NIC 등록: {nicData}");
            }
            else if(packetRcv.packetType == PacketType.UID)
            {
                //패킷 복호화
                byte[] decrypt = clientSession.crypto.AesGcmDecrypt(packetRcv.IV, packetRcv.data, packetRcv.tag);

                string uidData = Encoding.UTF8.GetString(decrypt);
                clientSession.UID = uidData;
                Console.WriteLine($"클라이언트 {clientSession.Id} NIC 등록: {uidData}");
            }
            else
            {
                _serverManager.Sessions.Remove(clientSession);
                return false;
            }

            if (clientSession.Id != null && clientSession.UID != null && clientSession.NIC != null)
            {
                return true;
            }

            goto RCV;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }
}