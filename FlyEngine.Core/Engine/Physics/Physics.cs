using System.Diagnostics;
using System.Numerics;
using FlyEngine.Core.Components;
using JoltPhysicsSharp;
using Microsoft.Extensions.Logging;

namespace FlyEngine.Core.Physics;

public static class Physics
{
    private abstract class PhysicsClass;
    
    private static readonly ILogger Logger = new Logger<PhysicsClass>(LoggerFactory.Create(b => b.AddConsole()));
    
    public static PhysicsSystem System;
    public static BodyInterface BodyInterface;
    public static JobSystem JobSystem;
    
    public static ObjectLayerPairFilterTable ObjectLayerPairFilter = new(2);
    public static BroadPhaseLayerInterfaceTable BroadPhaseLayerInterface = new(2, 2);

    private static PhysicsSystemSettings _settings;
    
    public static class Layers
    {
        public static readonly ObjectLayer NonMoving = 0;
        public static readonly ObjectLayer Moving = 1;
    }
    
    protected static class BroadPhaseLayers
    {
        public static readonly BroadPhaseLayer NonMoving = 0;
        public static readonly BroadPhaseLayer Moving = 1;
    }

    public static void Init()
    {
        Foundation.SetTraceHandler(Console.WriteLine);

#if DEBUG
        Foundation.SetAssertFailureHandler((inExpression, inMessage, inFile, inLine) =>
        {
            var message = inMessage ?? inExpression;

            var outMessage = $"[JoltPhysics] Assertion failure at {inFile}:{inLine}: {message}";

            Debug.WriteLine(outMessage);

            throw new Exception(outMessage);
        });
#endif

        var foundation = Foundation.Init();
        Logger.LogInformation("Foundation initialized: {foundation}", foundation);
        if (!foundation)
            return;
        _settings = new PhysicsSystemSettings
        {
            MaxBodies = 65536,
            MaxBodyPairs = 65536,
            MaxContactConstraints = 65536,
            NumBodyMutexes = 0
        };
        SetupCollisionFiltering();
        
        JobSystem = new JobSystemThreadPool();
        System = new PhysicsSystem(_settings);

        BodyInterface = System.BodyInterface;
    }

    public static void Shutdown()
    {
        JobSystem.Dispose();
        System.Dispose();
        BodyInterface = BodyInterface.Null;
        Foundation.Shutdown();
    }
    
    private static void SetupCollisionFiltering()
    {
        ObjectLayerPairFilter.EnableCollision(Layers.NonMoving, Layers.Moving);
        ObjectLayerPairFilter.EnableCollision(Layers.Moving, Layers.Moving);
        
        BroadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.NonMoving, BroadPhaseLayers.NonMoving);
        BroadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.Moving, BroadPhaseLayers.Moving);

        ObjectVsBroadPhaseLayerFilterTable objectVsBroadPhaseLayerFilter = new(BroadPhaseLayerInterface, 2, ObjectLayerPairFilter, 2);

        _settings.ObjectLayerPairFilter = ObjectLayerPairFilter;
        _settings.BroadPhaseLayerInterface = BroadPhaseLayerInterface;
        _settings.ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerFilter;
    }
    
    public static BodyID CreateBody(Shape shape, Vector3 position, Quaternion rotation, ObjectLayer layer, MotionType motionType = MotionType.Static)
    {
        Logger.LogInformation("Creating body with data:" +
                              "{@shape}," +
                              "{@position}," +
                              "{@rotation}," +
                              "{@layer}," +
                              "{@motiontype}",
            shape,
            position,
            rotation,
            layer,
            motionType);
        var settings = new BodyCreationSettings(
            shape,
            position,
            rotation,
            motionType,
            layer);
        var body = BodyInterface.CreateAndAddBody(settings, Activation.Activate);
        return body.ID;
    }

    public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        hit = new RaycastHit();
        if (!System.NarrowPhaseQuery.CastRay(new Ray(origin, direction * maxDistance), out var rayCastResult))
            return false;
        hit.Point = origin + direction * (maxDistance * rayCastResult.Fraction);
        var findGameObject = Application.Scene?.Colliders.ToList()
            .Find(o => o.BodyId == rayCastResult.BodyID);
        if (findGameObject == null)
            return true;
        var collider = findGameObject.GetComponent<Collider>();
        if (collider != null)
            hit.Collider = collider;
        var rigidbody = findGameObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
            hit.Rigidbody = rigidbody;
        return true;
    }

    public static void SetPosition(BodyID id, Vector3 position)
    {
        BodyInterface.SetPosition(id, position, Activation.Activate);
    }
    
    public static Vector3 GetPosition(BodyID id)
    {
        return BodyInterface.GetPosition(id);
    }

    public static Quaternion GetRotation(BodyID id)
    {
        return BodyInterface.GetRotation(id);
    }
}
