using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private TcpClient server;
    private NetworkStream stream;

    public async Task Run(string username)
    {
        server = new TcpClient();

        try
        {
            await server.ConnectAsync("127.0.0.1", 39990);
            stream = server.GetStream();
            Console.WriteLine("Connected to server");
            
            // 로그인 메시지 전송
            await SendAsync($"LOGIN:{username}");

            // 로그인 응답 확인
            string loginResponse = await ReceiveAsync();
            Console.WriteLine($"서버 응답: {loginResponse}");

            if (loginResponse == "LOGIN_OK")
            {
                // 서버 메시지 루프 시작
                await ListenToServer();
            }
            else
            {
                Console.WriteLine("로그인 실패");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private async Task SendAsync(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(data, 0, data.Length);
    }

    private async Task<string> ReceiveAsync()
    {
        byte[] buffer = new byte[1024];
        int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytes);
    }

    private async Task ListenToServer()
    {
        while (server.Connected)
        {
            try
            {
                string msg = await ReceiveAsync();
                Console.WriteLine($"서버에서 수신: {msg}");

                if (msg == "PING")
                {
                    await SendAsync("PONG");
                    Console.WriteLine("PONG 응답 전송");
                }
                // 추가 명령들 여기서 처리 가능
            }
            catch
            {
                Console.WriteLine("서버와 연결 끊김");
                break;
            }
        }
    }
    
    public static async Task Main(string[] args)
    {
        Console.Write("유저 이름 입력: ");
        string username = Console.ReadLine();

        Program client = new Program();
        await client.Run(username);
    }
}