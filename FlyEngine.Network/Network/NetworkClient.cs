using FlyEngine.Core.Engine;
using FlyEngine.Network.Packets;
using FlyEngine.Network.Serializable;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Network;

public class NetworkClient(
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

    private TaskCompletionSource<bool> _connectionTcs = new();

    public int LocalPlayerId { get; private set; } = -1;

    protected override bool OnStart()
    {
        var start = NetManager.Start();
        NetManager.Connect(NetworkManager.Host, NetworkManager.Port, NetworkManager.Key);
        return start;
    }

    public override async Task<bool> StartAsync()
    {
        if (!CanStart()) return false;
        _connectionTcs = new TaskCompletionSource<bool>();
        SetupListeners();
        if (!OnStart()) return false;
        IsActive = true;
        CancellationToken = new CancellationTokenSource();
        Thread = new Thread(PollEvents);
        Thread.Start(CancellationToken.Token);
        await _connectionTcs.Task;
        return true;
    }

    protected override bool CanStart()
    {
        return Application.IsRunning && !IsActive && !NetworkManager.Server.IsActive;
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        base.OnPeerConnected(peer);
        _connectionTcs.TrySetResult(true);
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        base.OnPeerDisconnected(peer, disconnectInfo);
        Shutdown();
    }

    protected override void OnNetworkReceive(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        base.OnNetworkReceive(peer, reader, channel, deliveryMethod);
        var packet = (NetworkPacket)reader.GetByte();

        switch (packet)
        {
            case NetworkPacket.Welcome:
                var id = reader.GetInt();
                var players = MemoryPackSerializer.Deserialize<List<PlayerData>>(reader.GetRemainingBytes());
                LocalPlayerId = id;
                if (players == null) break;
                PlayersData.Clear();
                PlayersData.AddRange(players);
                break;
            case NetworkPacket.NetworkTransform:
                var syncData = MemoryPackSerializer.Deserialize<TransformPacket>(reader.GetRemainingBytes());
                var targetNetTransform = NetworkManager.FindNetworkTransform(syncData.NetworkObjectId);
                targetNetTransform?.ApplySync(syncData);
                break;
            case NetworkPacket.Rpc:
                break;
            case NetworkPacket.SyncVariable:
                break;
            case NetworkPacket.SpawnObject:
                break;
            case NetworkPacket.AddPlayer:
                var player = MemoryPackSerializer.Deserialize<PlayerData>(reader.GetRemainingBytes());
                if (player == null) break;
                PlayersData.Add(player);
                break;
            case NetworkPacket.RemovePlayer:
                var playerId = reader.GetInt();
                var findPlayer = PlayersData.Find(p => p.Id == playerId);
                if (findPlayer == null) break;
                PlayersData.Remove(findPlayer);
                break;
            default:
                _logger.LogWarning("Unhandled packet type {packetType}", packet);
                break;
        }
    }
}