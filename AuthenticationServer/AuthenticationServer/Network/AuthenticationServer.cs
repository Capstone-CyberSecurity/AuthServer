namespace AuthenticationServer.Network;

public class AuthenticationServer : IServer
{
    public event Action<ClientSession> OnAuthenticated;
    
    public void Init()
    {
        
    }

    public void Update()
    {
        
    }

    public void HandleSession(ClientSession clientSession)
    {
        Console.WriteLine("여기에 인증 관련 로직 추가");
        Task.Run(async () =>
        {
            
        });
    }
}