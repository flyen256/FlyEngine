using FlyEngine.Core.Components.Common;

namespace FlyEngine.Network;

public class NetworkObject : Behaviour
{
    public int Id { get; private set; } = -1;
    public int OwnerId { get; private set; } = -1;
    
    public bool IsSpawned => Id > -1;
    public bool IsServer =>
        NetworkManager.Instance != null &&
        NetworkManager.Instance.Server.IsActive;
    public bool IsHost =>
        NetworkManager.Instance != null &&
        NetworkManager.Instance.Server.IsHost;
    public bool IsOwnedByServer => OwnerId == -1;
    public bool IsLocalPlayer =>
        NetworkManager.Instance != null &&
        NetworkManager.Instance.IsConnected &&
        NetworkManager.Instance.LocalPlayerId == OwnerId;

    protected internal void SpawnSceneObject(int id, int ownerId = -1)
    {
        Id = id;
        OwnerId = ownerId;
        SyncNetworkObject();
    }

    protected internal void SyncNetworkObject()
    {
        
    }
}