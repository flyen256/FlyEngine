namespace FlyEngine.Core.ECS;

public interface IComponentPool
{
    public void Resize(int newCapacity);
    public void Remove(int entityId);
    
    public bool HasEntity(int entityId);
    
    public byte[] SerializePool();
    public void DeserializePool(byte[] bytes);
}