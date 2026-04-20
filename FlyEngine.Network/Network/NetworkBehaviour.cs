using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Network;

public class NetworkBehaviour : Behaviour
{
    public NetworkRole NetworkRole { get; set; }

    public NetworkObject? NetworkObject => GetComponent<NetworkObject>();

    public bool IsSpawned => NetworkObject is { IsSpawned: true };
    public bool IsServer => NetworkObject is { IsServer: true };
    public bool IsHost => NetworkObject is { IsHost: true };
    public bool IsOwnedByServer => NetworkObject is { OwnerId: -1 };
    public bool IsLocalPlayer => NetworkObject is { IsLocalPlayer: true };
}