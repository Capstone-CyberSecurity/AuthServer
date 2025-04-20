
public class Program
{
    public static void Main()
    {
        ServerManager serverManager = new ServerManager();
        
        Task.Run(() => serverManager.Start());

        while (true)
        {

        }
    }
}