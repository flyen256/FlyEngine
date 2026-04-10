using FlyEngine.Components.Common;
using LiteNetLib;

namespace FlyEngine.Network.Components;

public class NetworkManager : Behaviour
{
    public int MaxConnections = 10;
    public readonly int Port = 80;
    public string ServerIp = "localhost";
    public string Key = "SomeKey";

    private readonly EventBasedNetListener _listener;
    private readonly NetManager _netManager;

    public bool IsClient { get; private set; }
    public bool IsServer { get; private set; }

    public NetworkManager()
    {
        _listener = new EventBasedNetListener();
        _netManager = new NetManager(_listener);
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