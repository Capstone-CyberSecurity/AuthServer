namespace AuthenticationServer.Network;

public class ListenServer : IServer
{
    private TcpListener _listener;
    public event Action<TcpClient> OnClientConnected;
    
    public void Init()
    {
        _listener = new TcpListener(IPAddress.Any, 39990);
        _listener.Start();
        Console.WriteLine($"Listening on port {_listener.LocalEndpoint}");
    }

    public void Update()
    {
        //별도의 스레드 할당
        Task.Run(async () =>
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                OnClientConnected?.Invoke(client);
            }
        });
    }
}