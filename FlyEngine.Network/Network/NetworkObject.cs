using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Network;

public class NetworkObject : Behaviour
{
    public int Id { get; private set; } = -1;
    public int OwnerId { get; private set; } = -1;
    
    public bool IsSpawned => Id > -1;

    public void Spawn(int ownerId = -1)
    {
        
        OwnerId = ownerId;
    }
}