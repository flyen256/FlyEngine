using LiteNetLib;

namespace FlyEngine.Network;

public class NetworkClient(NetworkManager networkManager) : NetworkSide(networkManager)
{
    public override void Start()
    {
        IsActive = true;
        NetworkManager.IsClient = true;
    }

    public override void Shutdown()
    {
        
    }
}