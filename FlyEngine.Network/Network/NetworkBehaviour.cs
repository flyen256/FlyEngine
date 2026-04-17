using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Network;

public class NetworkBehaviour : Behaviour
{
    public NetworkRole NetworkRole { get; set; }
    
    public NetworkObject? NetworkObject => GetComponent<NetworkObject>();
}