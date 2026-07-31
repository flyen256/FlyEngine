using System.Numerics;
using System.Text.Json.Serialization;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Components;

public class Character : Behaviour
{
	public float HalfHeight { get; set; } = 0.5f;
	public float Radius { get; set; } = 0.4f;
	public float MaxSlopeAngle { get; set; } = 45f;

	private CharacterVirtual? _character;

	[JsonIgnore]
	public GroundState GroundState =>
		_character?.GroundState ?? GroundState.NotSupported;
	
	[JsonIgnore]
	public Vector3 Velocity = Vector3.Zero;

	public override void OnLoad()
	{
		var settings = new CharacterVirtualSettings
		{
			Shape = new CapsuleShape(HalfHeight, Radius),
			MaxSlopeAngle = MaxSlopeAngle,
			Up = Vector3.UnitY,
			PredictiveContactDistance = 0.1f
		};
		_character = new CharacterVirtual(settings, Transform.Position, Transform.Rotation, 0, Core.Physics.Physics.System);
	}

	public override void OnUpdate(float deltaTime)
	{
		if (_character == null) return;
		_character.LinearVelocity = Velocity;
		_character.ExtendedUpdate(
			(float)deltaTime,
			new ExtendedUpdateSettings(),
			Core.Physics.Physics.Layers.Moving,
			Core.Physics.Physics.System
		);
		
		Transform.Position = _character.Position;
	}
}