using System.Text.Json.Serialization;
using FlyEngine.Core.Assets;
using FlyEngine.Core.CustomAttributes;
using FlyEngine.Core.Renderer;

namespace FlyEngine.Core.Components;

public class MeshRenderer : Behaviour
{
    public Color AlbedoTint { get; set; } = Color.White;
    public float Metallic { get; set; }
    public float Smoothness { get; set; }
    
    public Material? Material { get; set; }
    
    [Serialize, ShowInInspector]
    private Mesh? _mesh = null;

    [JsonIgnore]
    public Mesh? Mesh => _mesh;

    public override void OnRender(float deltaTime)
    {
        if (_mesh == null || Application.Window == null || Application.Window.OpenGl == null) return;
        
        var pipeline = Application.Window.OpenGl.RenderPipeline;
        var modelMatrix = Transform.WorldMatrix;

        for (var i = 0; i < _mesh.SubMeshes.Count; i++)
        {
            var subMesh = _mesh.SubMeshes[i];
            pipeline.Submit(this, subMesh, modelMatrix);
        }
    }
}