using FlyEngine.Core.Engine;
using FlyEngine.Core.Engine.Components.Common;
using FlyEngine.Network.Packets;
using FlyEngine.Network.Serializable;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Network;

public class NetworkServer(
    NetworkManager networkManager,
    EventBasedNetListener listener,
    NetManager netManager,
    List<PlayerData> playersData,
    Dictionary<int, NetworkObject> networkObjects) :
    NetworkSide(
        networkManager,
        listener,
        netManager,
        playersData,
        networkObjects)
{
    private readonly ILogger _logger = new Logger<NetworkServer>(LoggerFactory.Create(builder => builder.AddConsole()));

    private readonly Dictionary<uint, GameObject> _playersObjects = new();

    public bool IsHost { get; private set; }

    protected override bool OnStart()
    {
        return NetManager.Start(NetworkManager.Port);
    }

    public override bool Start(object? param = null)
    {
        if (param is not bool boolParam) return base.Start(param);
        if (!base.Start(param))
            return false;
        IsHost = boolParam;
        if (IsHost)
            AddHostPlayer();
        return true;
    }

    protected override bool CanStart()
    {
        return Application.IsRunning && !IsActive && !NetworkManager.Client.IsActive;
    }

    private void AddHostPlayer()
    {
        if (PlayersData.Exists(p => p.IsHost)) return;
        PlayersData.Add(new PlayerData
        {
            Id = GetNextAvailableId(),
            PeerId = -1,
            IsHost = true
        });
    }

    private PlayerData? AddPlayer(NetPeer peer)
    {
        if (PlayersData.Exists(p => p.PeerId == peer.Id)) return null;
        var playerData = new PlayerData
        {
            Id = GetNextAvailableId(),
            PeerId = peer.Id,
            IsHost = false
        };
        PlayersData.Add(playerData);
        return playerData;
    }

    private uint GetNextAvailableId()
    {
        for (uint i = 0; i < NetworkManager.MaxConnections + 1; i++)
        {
            if (PlayersData.All(p => p.Id != i))
                return i;
        }
        return 0;
    }
    
    private uint? RemovePlayer(NetPeer peer)
    {
        var findPlayer = PlayersData.Find(p => p.PeerId == peer.Id);
        if (findPlayer == null) return null;
        var findPlayerId = findPlayer.Id;
        PlayersData.Remove(findPlayer);
        return findPlayerId;
    }

    protected override void OnConnectionRequest(ConnectionRequest request)
    {
        base.OnConnectionRequest(request);
        if (NetManager.ConnectedPeersCount > NetworkManager.MaxConnections)
        {
            var rejectData = new NetDataWriter();
            rejectData.Put("Server is full");
            request.Reject(rejectData);
            return;
        }
        if (NetworkManager.Key.Length > 0)
            request.AcceptIfKey(NetworkManager.Key);
        else
            request.Accept();
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        base.OnPeerConnected(peer);
        _logger.LogInformation("Connected peer with id {PeerId}", peer.Id);
        var addPlayer = AddPlayer(peer);
        var writer = new NetDataWriter();
        writer.Put((byte)NetworkPacket.Welcome);
        writer.Put(peer.Id);
        writer.Put(MemoryPackSerializer.Serialize(PlayersData));
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
        var connectedPeers = new List<NetPeer>();
        NetManager.GetConnectedPeers(connectedPeers);
        if (addPlayer == null) return;
        var addPlayerSerialized = MemoryPackSerializer.Serialize(addPlayer);
        var newPlayerWriter = new NetDataWriter();
        newPlayerWriter.Put((byte)NetworkPacket.AddPlayer);
        newPlayerWriter.Put(addPlayerSerialized);
        foreach (var netPeer in connectedPeers.Where(p => p.Id != peer.Id))
            netPeer.Send(newPlayerWriter, DeliveryMethod.ReliableOrdered);
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        base.OnPeerDisconnected(peer, disconnectInfo);
        _logger.LogInformation("Disconnected peer with id {PeerId}, reason {Reason}", peer.Id, disconnectInfo.Reason);
        var removePlayerId = RemovePlayer(peer);
        if (removePlayerId == null) return;
        var writer = new NetDataWriter();
        writer.Put((byte)NetworkPacket.RemovePlayer);
        writer.Put(removePlayerId.Value);
        var connectedPeers = new List<NetPeer>();
        NetManager.GetConnectedPeers(connectedPeers);
        foreach (var netPeer in connectedPeers.Where(p => p.Id != peer.Id))
            netPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    protected override void OnNetworkReceive(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        base.OnNetworkReceive(peer, reader, channel, deliveryMethod);
        var packet = (NetworkPacket)reader.GetByte();

        switch (packet)
        {
            case NetworkPacket.NetworkTransform:
                var syncRawData = reader.GetRemainingBytes();
                var syncData = MemoryPackSerializer.Deserialize<TransformPacket>(syncRawData);
                var targetNetTransform = NetworkManager.FindNetworkTransform(syncData.NetworkObjectId);
                targetNetTransform?.ApplySync(syncData);
                var connectedPeers = new List<NetPeer>();
                NetManager.GetConnectedPeers(connectedPeers);
                foreach (var netPeer in connectedPeers.Where(p => p.Id != peer.Id))
                    netPeer.Send(syncRawData, deliveryMethod);
                break;
            case NetworkPacket.Rpc:
                break;
            default:
                _logger.LogWarning("Unhandled packet type {packetType}", packet);
                break;
        }
    }

    protected override void OnShutdown()
    {
        IsHost = false;
    }
}