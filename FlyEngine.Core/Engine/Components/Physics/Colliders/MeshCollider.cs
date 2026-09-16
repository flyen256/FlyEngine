using FlyEngine.Core.CustomAttributes;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class MeshCollider : Collider
{
    [Serialize, ShowInInspector]
    private MeshShapeBuildQuality _buildQuality = MeshShapeBuildQuality.FavorRuntimePerformance;
    
    private MeshRenderer? MeshRenderer => GetComponent<MeshRenderer>();

    protected override void CreateBody(MotionType motionType)
    {
        if (MeshRenderer?.Mesh == null) return;
        var mesh = MeshRenderer.Mesh;
        var vertices = mesh.Vertices.Select(v => v.Position).ToArray();

        var triangleCount = mesh.Indices.Count / 3;
        var triangles = new IndexedTriangle[triangleCount];
        
        for (var i = 0; i < triangleCount; i++)
        {
            triangles[i] = new IndexedTriangle(
                mesh.Indices[i * 3],
                mesh.Indices[i * 3 + 1],
                mesh.Indices[i * 3 + 2]
            );
        }

        var settings = new MeshShapeSettings(vertices.AsSpan(), triangles.AsSpan());
        settings.BuildQuality = _buildQuality;
        BodyId = Physics.Physics.CreateBody(settings.Create(), Transform.Position, Transform.Rotation,
            Physics.Physics.Layers.Moving, motionType);
    }
}