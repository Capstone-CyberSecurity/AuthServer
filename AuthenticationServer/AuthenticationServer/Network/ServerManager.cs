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
        _listenServer.OnClientConnected += (client) =>
        {
            ClientSession session = new ClientSession(client);
            if(_authServer.LoginSession(session))
                Sessions.Add(session);
            //_heartbeatServer.MonitorSession(session);
        };
    }

    public void Start()
    {
        _listenServer.Init();
        _authServer.Init();
        _heartbeatServer.Init();

        while (true)
        {
            _listenServer.Update();
        }

        return;
    }
}