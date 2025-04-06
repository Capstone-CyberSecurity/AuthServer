namespace AuthenticationServer.Utility;

public class Singleton<T> where T : class, new()
{
    private static T? _instance = null;
    private static readonly object _lock = new object();

    protected Singleton() { }
    
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }

            return _instance;
        }
    }
}