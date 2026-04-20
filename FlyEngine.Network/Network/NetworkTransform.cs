using System.Numerics;
using FlyEngine.Network.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;

namespace FlyEngine.Network;

public class NetworkTransform : NetworkBehaviour
{
    public float InterpolationSpeed { get; set; } = 15f;
    
    private float _lastSendTime;

    private Vector3 _targetPosition;
    private Vector3 _targetScale;
    private Quaternion _targetRotation;

    public override void OnLoad()
    {
        _targetPosition = Transform.Position;
        _targetRotation = Transform.Rotation;
    }

    public override void OnUpdate(double deltaTime)
    {
        if (NetworkObject is not { IsSpawned: true }) return;

        if (IsLocalPlayer || (IsServer && IsOwnedByServer))
            UpdateAuthority((float)deltaTime);
        else
            UpdateRemote((float)deltaTime);
    }

    private void UpdateAuthority(float deltaTime)
    {
        if (NetworkManager.Instance == null) return;
        _lastSendTime += deltaTime;
        if (!(_lastSendTime >= 1f / NetworkManager.Instance.Tps)) return;
        _lastSendTime = 0;
        SendTransform();
    }

    private void SendTransform()
    {
        if (NetworkObject == null || NetworkManager.Instance == null) return;
        var packet = new TransformPacket
        {
            NetworkObjectId = NetworkObject.Id,
            Position = Transform.Position,
            Rotation = Transform.Rotation,
            Scale = Transform.Scale
        };

        var data = MemoryPackSerializer.Serialize(packet);
        var writer = new NetDataWriter();
        writer.Put((byte)NetworkPacket.NetworkTransform);
        writer.Put(data);

        NetworkManager.Instance.Broadcast(writer, DeliveryMethod.Unreliable);
    }

    private void UpdateRemote(float deltaTime)
    {
        Transform.Position = Vector3.Lerp(Transform.Position, _targetPosition, deltaTime * InterpolationSpeed);
        Transform.Rotation = Quaternion.Slerp(Transform.Rotation, _targetRotation, deltaTime * InterpolationSpeed);
        Transform.Scale = Vector3.Lerp(Transform.Position, _targetScale, deltaTime * InterpolationSpeed);
    }

    public void ApplySync(TransformPacket packet)
    {
        _targetPosition = packet.Position;
        _targetRotation = packet.Rotation;
        _targetScale = packet.Scale;
    }
}