namespace AuthenticationServer.Network;

public class AuthenticationServer(ServerManager serverManager) : IServer
{
    private ServerManager _serverManager = serverManager;
    public event Action<ClientSession> OnAuthenticated;

    public void Init()
    {
        
    }

    public void Update()
    {
        
    }

    public bool LoginSession(ClientSession clientSession)
    {
        Console.WriteLine("AuthServer.cs 로그인 관련 작업");
        try
        {
            Task.Run(async () =>
            {
                string message = await clientSession.ReceiveAsync();
                Console.WriteLine(clientSession.Client.Client.RemoteEndPoint + " [Login Session] " + message);
                if (message.Contains("LOGIN"))
                {
                    await clientSession.SendAsync("LOGIN_OK");
                    Console.WriteLine(clientSession.Client.Client.RemoteEndPoint + " [Login Session] Accept Login");
                }
            });
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    public void HandleSession(ClientSession clientSession)
    {
        Console.WriteLine("AuthServer.cs 여기에 인증 관련 로직 추가");
        Task.Run(async () =>
        {
            
        });
    }
}