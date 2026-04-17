using System.Numerics;
using FlyEngine.Core.Engine.Components.Common;

namespace FlyEngine.Core.Engine.Behaviours;

public class RotationTimer : Behaviour
{
    public Vector3 Axis { get; set; } = Vector3.UnitX;
    
    public override void OnUpdate(double deltaTime)
    {
        var time = Application.Instance.Window.Time;
        Transform.Rotation = Quaternion.CreateFromAxisAngle(Axis, (float)(time * 2f));
    }
}