namespace FlyEngine.Core.ECS.Systems;

public interface ISystem
{
    public void Update(World world, float deltaTime);
}