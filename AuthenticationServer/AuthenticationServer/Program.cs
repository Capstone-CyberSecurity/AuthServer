
public class Program
{
    public static async Task Main()
    {
        ServerManager serverManager = new ServerManager();
        Database database = new Database();

        Console.WriteLine("DB 접속하시겠습니까? Yes: 1, No: 2");
        int dbConnect = Console.Read();
        if(dbConnect == 1)
        {
            await database.ConnectAsync("127.0.0.1", 9999);

            await database.SendAsync("{\"source\":\"Hello\"}");
        }

        Task.Run(() => serverManager.Start());

        while (true)
        {

        }
    }
}