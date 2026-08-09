using MemoryPack;

namespace FlyEngine.Core.ECS;

[MemoryPackable]
public readonly partial struct Entity(int id, int version) : IEquatable<Entity>
{
    [MemoryPackInclude]
    public readonly int Id = id;
    [MemoryPackInclude]
    public readonly int Version = version;

    [MemoryPackIgnore]
    public bool IsNull => Id < 0;

    public bool Equals(Entity other) => Id == other.Id && Version == other.Version;
    public override bool Equals(object? obj) => obj is Entity other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Id, Version);
    
    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}