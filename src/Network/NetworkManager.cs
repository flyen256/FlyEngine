namespace FlyEngine.Network;

public class NetworkManager
{
    public int MaxConnections { get; set; } = 10;
    public int Port { get; set; } = 80;
    public string ServerIp { get; set; } = "localhost";
    public string Key { get; set; } = "test";
    public float Tps { get; set; } = 30f;

    public readonly Application Application;
    
    public readonly NetworkServer Server;
    public readonly NetworkClient Client;

    public bool IsClient { get; set; }
    public bool IsServer { get; set; }

    public NetworkManager(Application application)
    {
        Application = application;
        Server = new NetworkServer(this);
        Client = new NetworkClient(this);
    }

    public void Shutdown()
    {
        if (Server.IsActive)
            Server.Shutdown();
    }
}