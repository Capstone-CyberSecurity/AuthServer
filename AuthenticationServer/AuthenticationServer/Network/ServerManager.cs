namespace AuthenticationServer.Network;

public class ServerManager : Singleton<ServerManager>
{
    private ListenServer _listenServer;
    private AuthenticationServer _authServer;
    private HeartbeatServer _heartbeatServer;

    //Lock 처리를 통해서 중복 접근 방지
    public List<ClientSession> Sessions = new List<ClientSession>();

    public ServerManager()
    {
        _listenServer = new ListenServer(this);
        _authServer = new AuthenticationServer(this);
        _heartbeatServer = new HeartbeatServer(this);
        
        Bind();
    }

    private void Bind()
    {
        //이벤트 Binding 시도

        //처음 클라이언트가 접속했을 때
        _listenServer.OnClientConnected += async (client) =>
        {
            ClientSession session = new ClientSession(client);
            if(await _listenServer.LoginSession(session))
                Sessions.Add(session);
            //_heartbeatServer.MonitorSession(session);
        };
    }

    public async Task Start()
    {
        _listenServer.Init();
        _authServer.Init();
        _heartbeatServer.Init();

        /*await _listenServer.Update();
            await _authServer.Update();
            await _heartbeatServer.Update();
             */
        // Await 쓰면 비동기지만 return이 나올 때까지 계속 돌기 때문에 Thread 계속 점유 문제 존재
        // 각 Update를 병렬로 실행
        var listen = _listenServer.Update();
        var auth = _authServer.Update();
        var heartbeat = _heartbeatServer.Update();

        // 모두 비동기로 병렬 실행되게 대기
        await Task.WhenAll(listen, auth, heartbeat);
    }
}