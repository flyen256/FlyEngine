using LiteNetLib;

namespace FlyEngine.Network;

public class NetworkServer(NetworkManager networkManager) : NetworkSide(networkManager)
{
    public override void Start()
    {
        NetManager.Start(NetworkManager.Port);
        IsActive = true;
        NetworkManager.IsServer = true;
    }

    public override void Shutdown()
    {
        NetManager.Stop();
    }
}