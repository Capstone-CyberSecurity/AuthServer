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
        
    }

    public void HandleSession(ClientSession clientSession)
    {
        Console.WriteLine("AuthServer.cs 여기에 인증 관련 로직 추가");
        Task.Run(async () =>
        {
            
        });
    }
}