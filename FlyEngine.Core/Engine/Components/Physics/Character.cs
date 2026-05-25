using System.Numerics;
using FlyEngine.Core.Components.Common;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Engine.Components.Physics;

public class Character : Behaviour
{
	public float HalfHeight { get; set; } = 0.5f;
	public float Radius { get; set; } = 0.4f;
	public float MaxSlopeAngle { get; set; } = 45f;

	private CharacterVirtual _character = null!;

	public override void OnLoad()
	{
		var settings = new CharacterVirtualSettings
		{
			Shape = new CapsuleShape(HalfHeight, Radius),
			MaxSlopeAngle = MaxSlopeAngle,
			Up = Vector3.UnitY,
			PredictiveContactDistance = 0.1f
		};
		_character = new CharacterVirtual(settings, Transform.Position, Transform.Rotation, 0, Core.Physics.System);
	}

	public override void OnUpdate(double deltaTime)
	{
		var currentVelocity = _character.LinearVelocity;

		const float gravity = -9.81f;
		var verticalVelocity = currentVelocity.Y + (gravity * (float)deltaTime);

		var finalVelocity = new Vector3(0.0f, verticalVelocity, 0.0f);
		
		_character.LinearVelocity = finalVelocity;
		
		_character.ExtendedUpdate(
			(float)deltaTime,
			new ExtendedUpdateSettings(),
			Core.Physics.Layers.Moving,
			Core.Physics.System
		);
	}
}