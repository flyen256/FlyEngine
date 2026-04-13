using System.Diagnostics;
using System.Numerics;
using JoltPhysicsSharp;

namespace FlyEngine.Physics;

public class Physics
{
    public readonly PhysicsSystem System;
    public readonly BodyInterface BodyInterface;
    public readonly JobSystem JobSystem;

    private PhysicsSystemSettings _settings; 
    
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

    public Physics(int maxBodies = 65536, int maxBodyPairs = 65536, int maxContactConstraints = 65536)
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

        if (!Foundation.Init())
            return;
        _settings = new PhysicsSystemSettings
        {
            MaxBodies = maxBodies,
            MaxBodyPairs = maxBodyPairs,
            MaxContactConstraints = maxContactConstraints,
            NumBodyMutexes = 0,
        };
        SetupCollisionFiltering();
        
        JobSystem = new JobSystemThreadPool();
        System = new PhysicsSystem(_settings);

        BodyInterface = System.BodyInterface;
    }
    
    protected void SetupCollisionFiltering()
    {
        ObjectLayerPairFilterTable objectLayerPairFilter = new(2);
        objectLayerPairFilter.EnableCollision(Layers.NonMoving, Layers.Moving);
        objectLayerPairFilter.EnableCollision(Layers.Moving, Layers.Moving);

        BroadPhaseLayerInterfaceTable broadPhaseLayerInterface = new(2, 2);
        broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.NonMoving, BroadPhaseLayers.NonMoving);
        broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.Moving, BroadPhaseLayers.Moving);

        ObjectVsBroadPhaseLayerFilterTable objectVsBroadPhaseLayerFilter = new(broadPhaseLayerInterface, 2, objectLayerPairFilter, 2);

        _settings.ObjectLayerPairFilter = objectLayerPairFilter;
        _settings.BroadPhaseLayerInterface = broadPhaseLayerInterface;
        _settings.ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerFilter;
    }
    
    public BodyID CreateBody(Shape shape, Vector3 position, Quaternion rotation, ObjectLayer layer, MotionType motionType = MotionType.Static)
    {
        var settings = new BodyCreationSettings(
            shape,
            position,
            rotation,
            motionType,
            layer);
        var body = BodyInterface.CreateAndAddBody(settings, Activation.Activate);
        return body.ID;
    }

    public void SetPosition(BodyID id, Vector3 position)
    {
        BodyInterface.SetPosition(id, position, Activation.Activate);
    }
    
    public Vector3 GetPosition(BodyID id)
    {
        return BodyInterface.GetPosition(id);
    }

    public Quaternion GetRotation(BodyID id)
    {
        return BodyInterface.GetRotation(id);
    }
}