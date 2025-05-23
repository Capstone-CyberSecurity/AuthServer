
public class Program
{
    public static async Task Main()
    {
        ServerManager serverManager = new ServerManager();

        Console.WriteLine("DB 접속하시겠습니까? Yes: 1, No: 2");
        int dbConnect = Console.Read();
        if(dbConnect == 49)
        {
            Console.WriteLine("DB 접속중..");

            await Database.Instance.ConnectAsync("127.0.0.1", 9999);
        }

        Task.Run(() => serverManager.Start());

        while (true)
        {

        }
    }
}