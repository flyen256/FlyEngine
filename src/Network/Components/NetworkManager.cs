using Flyeng;
using LiteNetLib;

namespace Flyeng;

public class NetworkManager : Behaviour
{
    public int MaxConnections = 10;
    public int Port = 80;
    public string ServerIp = "localhost";
    public string Key = "SomeKey";

    private EventBasedNetListener _listener;
    private NetManager _netManager;

    public bool IsClient { get; private set; }
    public bool IsServer { get; private set; }

    public NetworkManager()
    {
        _listener = new();
        _netManager = new(_listener);
    }

    public void StartServer()
    {
        IsServer = true;
        _netManager.Start(Port);
        
    }

    public void StartClient()
    {
        IsClient = true;
    }

    public void Shutdown()
    {
        _netManager.Stop();
        IsServer = false;
        IsClient = false;
    }
}