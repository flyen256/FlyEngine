using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Network.Serializable;
using LiteNetLib;

namespace FlyEngine.Network;

public class NetworkManager : Behaviour
{
    public static NetworkManager? Instance { get; private set; }
    public uint MaxConnections { get; set; } = 10;
    public uint Tps { get; set; } = 30;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 80;
    public string Key { get; set; } = "";
    public bool AutoCreatePlayer { get; set; } = false;
    public Func<GameObject>? PlayerPrefab { get; set; }

    public readonly Dictionary<string, NetworkObject> NetworkObjects = [];

    public Application? Application;

    public NetworkServer Server;
    public NetworkClient Client;

    private readonly NetManager _serverNetManager;
    private readonly NetManager _clientNetManager;
    private readonly EventBasedNetListener _serverListener;
    private readonly EventBasedNetListener _clientListener;
    
    private readonly List<PlayerData> _playersData = [];

    public List<NetPeer> ConnectedPeers
    {
        get
        {
            if (!Client.IsActive && !Server.IsActive) return [];
            var connectedPeers = new List<NetPeer>();
            if (Server.IsActive)
            {
                _serverNetManager.GetConnectedPeers(connectedPeers);
                return connectedPeers;
            }

            _clientNetManager.GetConnectedPeers(connectedPeers);
            return connectedPeers;
        }
    }
    
    public IReadOnlyList<PlayerData> PlayersData => _playersData;

    public bool Initialized => Application != null;

    public event Action<NetPeer, DisconnectInfo> OnPeerDisconnected
    {
        add
        {
            if (Server.IsActive)
                Server.OnPeerDisconnectedEvent += value;
            else if (Client.IsActive)
                Client.OnPeerDisconnectedEvent += value;
        }
        remove
        {
            if (Server.IsActive)
                Server.OnPeerDisconnectedEvent -= value;
            else if (Client.IsActive)
                Client.OnPeerDisconnectedEvent -= value;
        }
    }

    public NetworkManager()
    {
        Instance = this;
        _serverListener = new EventBasedNetListener();
        _clientListener = new EventBasedNetListener();
        _serverNetManager = new NetManager(_serverListener);
        _clientNetManager = new NetManager(_clientListener);
        Application = Application.Instance;
        Server = new NetworkServer(this, Application, _serverListener, _serverNetManager, _playersData);
        Client = new NetworkClient(this, Application, _clientListener, _clientNetManager, _playersData);
    }

    public void Shutdown()
    {
        if (!Application.IsRunning) return;
        if (Server is { IsActive: true })
            Server.Shutdown();
        if (Client is { IsActive: true })
            Client.Shutdown();
    }
}