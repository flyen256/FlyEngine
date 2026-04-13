using LiteNetLib;

namespace FlyEngine.Network;

public abstract class NetworkSide
{
    protected readonly NetworkManager NetworkManager;

    protected readonly EventBasedNetListener Listener;
    protected readonly NetManager NetManager;

    protected NetworkSide(NetworkManager networkManager)
    {
        NetworkManager = networkManager;
        Listener = new EventBasedNetListener();
        NetManager = new NetManager(Listener);
    }
    
    public bool IsActive { get; protected set; }

    public abstract void Start();
    public abstract void Shutdown();
}
