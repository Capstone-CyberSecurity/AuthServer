namespace AuthenticationServer.Network;

public class HeartbeatServer(ServerManager serverManager) : IServer
{
    private ServerManager _serverManager = serverManager;
    public event Action<ClientSession> OnHeartbeat;

    public void Init()
    {
        
    }

    public async Task Update()
    {
        while (true)
        {
            // 현재 세션 리스트 복사 (foreach 중간에 삭제 방지)
            var sessions = _serverManager.Sessions.ToList();

            // MonitorSession을 병렬로 실행
            var tasks = sessions.Select(session => MonitorSession(session));
            await Task.WhenAll(tasks);

            // 일정 간격 대기 (예: 5초)
            await Task.Delay(5000);
        }
    }

    public async Task MonitorSession(ClientSession clientSession)
    {
        try
        {
            using (var cts = new CancellationTokenSource(7000))
            {
                //패킷 직렬화 및 암호화
                byte[] aesIV = Crypto.GenerateRandomIV();
                (byte[] message, byte[] tag) = clientSession.crypto.AesGcmEncrypt(aesIV, new byte[1] { 0x48 });

                Packet sendPacket = new Packet { packetType = PacketType.HEART, IV = aesIV, tag = tag, data = message };

                await clientSession.SendBytesAsync(sendPacket.ToBytes());
                Console.WriteLine("[Heartbeat] Send Heart to " + clientSession.Id);

                //핑 응답 확인
                byte[] rcvMessage = await clientSession.ReceiveBytesAsync(cts.Token);
                Packet rcvPacket = Packet.FromBytes(rcvMessage);
                Console.WriteLine(rcvPacket.packetType);

                if (rcvPacket.packetType == PacketType.BEAT)
                {
                    Console.WriteLine("[Heartbeat] Get Response: " + clientSession.Id);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Session Disconnected or Timeout: " + clientSession.Id + " " + ex.Message);
            _serverManager.Sessions.Remove(clientSession);
        }
    }
}