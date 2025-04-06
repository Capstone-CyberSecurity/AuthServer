namespace AuthenticationServer.Network;

public class ServerManager : Singleton<ServerManager>
{
    private ListenServer _listenServer;
    private AuthenticationServer _authServer;
    private HeartbeatServer _heartbeatServer;

    public ServerManager()
    {
        _listenServer = new ListenServer();
        _authServer = new AuthenticationServer();
        _heartbeatServer = new HeartbeatServer();
        
        Bind();
    }

    private void Bind()
    {
        //이벤트 Binding 시도
        _listenServer.OnClientConnected += (client) =>
        {
            var session = new ClientSession(client);
            _authServer.HandleSession(session);
            _heartbeatServer.MonitorSession(session);
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