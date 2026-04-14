namespace FlyEngine.Core.Network;

public class NetworkClient(NetworkManager networkManager) : NetworkSide(networkManager)
{
    public override void Start()
    {
        NetManager.Start();
        NetManager.Connect(NetworkManager.Host, NetworkManager.Port, NetworkManager.Key);
        IsActive = true;
        NetworkManager.IsClient = true;
    }

    public override void Shutdown()
    {
        NetManager.Stop();
    }
}