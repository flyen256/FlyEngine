using FlyEngine.Core.Engine;
using FlyEngine.Network.Serializable;
using LiteNetLib;
using LiteNetLib.Utils;

namespace FlyEngine.Network;

public abstract class NetworkSide
{
    protected readonly NetworkManager NetworkManager;

    protected readonly EventBasedNetListener Listener;
    protected readonly NetManager NetManager;

    protected Thread Thread;
    protected CancellationTokenSource CancellationToken = new();

    protected readonly List<PlayerData> PlayersData;
    protected readonly Dictionary<int, NetworkObject> NetworkObjects;
    
    public bool IsActive { get; protected set; }
    
    public event Action<NetPeer>? OnPeerConnectedEvent;
    public event Action<NetPeer, DisconnectInfo>? OnPeerDisconnectedEvent;

    protected NetworkSide(
        NetworkManager networkManager,
        EventBasedNetListener listener,
        NetManager netManager,
        List<PlayerData> playersData,
        Dictionary<int, NetworkObject> networkObjects)
    {
        NetworkManager = networkManager;
        Listener = listener;
        NetManager = netManager;
        Thread = new Thread(PollEvents);
        PlayersData = playersData;
        NetworkObjects = networkObjects;
    }

    protected void PollEvents(object? cancellationToken)
    {
        if (cancellationToken is not CancellationToken token) return;
        while (IsActive)
        {
            if (token.IsCancellationRequested) return;
            NetManager.PollEvents();
            Thread.Sleep((int)(1000f / NetworkManager.Tps));
        }
    }

    public virtual bool Start(object? param = null)
    {
        if (!CanStart()) return false;
        SetupListeners();
        if (!OnStart()) return false;
        IsActive = true;
        CancellationToken = new CancellationTokenSource();
        Thread = new Thread(PollEvents);
        Thread.Start(CancellationToken.Token);
        return true;
    }

    protected abstract bool CanStart();

    protected abstract bool OnStart();

    public virtual Task<bool> StartAsync()
    {
        return Task.FromResult(Start());
    }

    protected void SetupListeners()
    {
        Listener.ConnectionRequestEvent += OnConnectionRequest;
        Listener.PeerConnectedEvent += OnPeerConnected;
        Listener.PeerDisconnectedEvent += OnPeerDisconnected;
        Listener.NetworkReceiveEvent += OnNetworkReceive;
    }

    protected void RemoveListeners()
    {
        Listener.ConnectionRequestEvent -= OnConnectionRequest;
        Listener.PeerConnectedEvent -= OnPeerConnected;
        Listener.PeerDisconnectedEvent -= OnPeerDisconnected;
        Listener.NetworkReceiveEvent -= OnNetworkReceive;
    }

    public void Shutdown()
    {
        if (!Application.IsRunning || !IsActive) return;
        IsActive = false;
        PlayersData.Clear();
        NetManager.Stop();
        CancellationToken.Cancel();
        RemoveListeners();
        OnShutdown();
    }

    protected virtual void OnShutdown() {}
    
    protected virtual void OnConnectionRequest(ConnectionRequest request) {}
    protected virtual void OnPeerConnected(NetPeer peer) =>
        OnPeerConnectedEvent?.Invoke(peer);
    protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) =>
        OnPeerDisconnectedEvent?.Invoke(peer, disconnectInfo);
    protected virtual void OnNetworkReceive(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod) {}
}