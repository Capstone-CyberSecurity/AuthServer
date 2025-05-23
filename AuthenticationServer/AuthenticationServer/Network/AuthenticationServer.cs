namespace AuthenticationServer.Network;

public class AuthenticationServer(ServerManager serverManager) : IServer
{
    private ServerManager _serverManager = serverManager;
    public event Action<ClientSession> OnAuthenticated;

    public void Init()
    {
        
    }

    public async Task Update()
    {
        while (true)
        {
            // 일정 간격 대기 (예: 2초)
            await Task.Delay(2000);

            // 현재 세션 리스트 복사 (foreach 중간에 삭제 방지)
            var sessions = _serverManager.Sessions.ToList();

            // HandleSession을 병렬로 실행
            var tasks = sessions.Select(session => HandleSession(session));
            await Task.WhenAll(tasks);
        }
    }

    public async Task HandleSession(ClientSession clientSession)
    {
        try
        {
            if(clientSession.Id == "bec")
            {
                string targetHash = clientSession.HASH;
                

                //DB서버에 메시지 전송: Hash값
                string json = JsonSerializer.Serialize(new { uid_mac_hash = targetHash });
                Console.WriteLine("[DB] 해싱 비교 데이터 전송: " + json);
                await Database.Instance.SendAsync(json);

                //DB서버에 NIC_COMPUTER 받아오기
                byte[] dbMessage = await Database.Instance.ReceiveAsync();

                string targetMac = Encoding.UTF8.GetString(dbMessage);

                Console.WriteLine("[DB] 사용자 매칭 결과: " + targetMac);

                await Database.Instance.ConnectAsync("127.0.0.1", 9999);

                if (targetMac.Contains("Non"))
                    return;

                //반복문 돌려서 같은 nic 쓰는 사람 찾기
                ClientSession nicMatchCom = searchClient(targetMac);
                if (nicMatchCom != null)
                {
                    //패킷 직렬화 및 암호화
                    byte[] aesIV = Crypto.GenerateRandomIV();
                    (byte[] message, byte[] tag) = nicMatchCom.crypto.AesGcmEncrypt(aesIV, new byte[4] { 0x4F, 0x70, 0x65, 0x6E });

                    Packet sendPacket = new Packet { packetType = PacketType.ORDER_TO_CLI, IV = aesIV, tag = tag, data = message };

                    await nicMatchCom.SendBytesAsync(sendPacket.ToBytes());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클라이언트 인증 오류: {ex.Message}");
        }
    }

    private ClientSession searchClient(string nic_computer)
    {
        //클라 테이블에서 검색 진행해서 추출
        var sessions = _serverManager.Sessions.ToList();
        foreach (var session in sessions)
        {
            if (session.NIC == nic_computer)
            {
                return session;
            }
        }
        return null;
    }

    private bool isNicMatchClient(ClientSession session, string nic_computer)
    {
        if (session.NIC == nic_computer)
        {
            return true;
        }
        return false;
    }
}