namespace AuthenticationServer.Network;

public class HeartbeatServer(ServerManager serverManager) : IServer
{
    private ServerManager _serverManager = serverManager;

    public void Init()
    {
        
    }

    public void Update()
    {
        
    }

    public void MonitorSession(ClientSession session)
    {
        Console.WriteLine("Heartbeat.cs 초마다 핑 보내서 보내는거 실패하면 해제 상태 확인");
        Task.Run(async () =>
        {
            while (session.Client.Connected)
            {
                
            }
        });
    }
}