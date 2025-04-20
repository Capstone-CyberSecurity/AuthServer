namespace AuthenticationServer.Network;

public interface IServer
{
    public void Init();
    public Task Update();
}