using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Assets;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Network.Serializable;
using LiteNetLib;
using LiteNetLib.Utils;

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
    public Prefab? PlayerPrefab { get; set; }

    public readonly NetworkServer Server;
    public readonly NetworkClient Client;

    public IReadOnlyDictionary<int, NetworkObject> NetworkObjects => _networkObjects;
    public IReadOnlyList<PlayerData> PlayersData => _playersData;

    private readonly Dictionary<int, NetworkObject> _networkObjects = [];
    private readonly List<PlayerData> _playersData = [];

    private readonly NetManager _serverNetManager;
    private readonly NetManager _clientNetManager;

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

    public int LocalPlayerId
    {
        get
        {
            if (!Client.IsActive && !Server.IsActive) return -1;
            if (Server.IsActive)
                return -1;
            if (Client.IsActive)
                return Client.LocalPlayerId;
            return -1;
        }
    }

    public bool IsConnected => Server.IsActive || Client.IsActive;

    public NetworkManager()
    {
        Instance = this;
        var serverListener = new EventBasedNetListener();
        var clientListener = new EventBasedNetListener();
        _serverNetManager = new NetManager(serverListener);
        _clientNetManager = new NetManager(clientListener);
        Server = new NetworkServer(this, serverListener, _serverNetManager, _playersData,
            _networkObjects);
        Client = new NetworkClient(this, clientListener, _clientNetManager, _playersData,
            _networkObjects);
    }

    public void Shutdown()
    {
        if (!Application.IsRunning) return;
        if (Server is { IsActive: true })
            Server.Shutdown();
        if (Client is { IsActive: true })
            Client.Shutdown();
    }
    
    public void Broadcast(NetDataWriter writer, DeliveryMethod deliveryMethod)
    {
        if (Server.IsActive)
        {
            var peers = new List<NetPeer>();
            _serverNetManager.GetConnectedPeers(peers);
            foreach (var peer in peers)
                peer.Send(writer, deliveryMethod);
        }
        else if (Client.IsActive)
            _clientNetManager.FirstPeer?.Send(writer, deliveryMethod);
    }

    public NetworkTransform? FindNetworkTransform(int networkObjectId)
    {
        if (!NetworkObjects.TryGetValue(networkObjectId, out var networkObject)) return null;
        return networkObject.GetComponent<NetworkTransform>();
    }
}