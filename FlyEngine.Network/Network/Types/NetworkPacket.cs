namespace FlyEngine.Network;

public enum NetworkPacket
{
    Welcome = 0,
    NetworkTransform = 1,
    Rpc = 2,
    SyncVariable = 3,
    SpawnObject = 4,
    AddPlayer = 5,
    RemovePlayer = 6
}