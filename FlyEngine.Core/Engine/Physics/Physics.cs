using System.Diagnostics;
using System.Numerics;
using FlyEngine.Core.Engine.Cast;
using FlyEngine.Core.Engine.Components;
using FlyEngine.Core.Engine.Components.Colliders;
using JoltPhysicsSharp;

namespace FlyEngine.Core.Engine;

public static class Physics
{
    public static readonly PhysicsSystem System;
    public static readonly BodyInterface BodyInterface;
    public static readonly JobSystem JobSystem;

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

    static Physics()
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
    
    private static void SetupCollisionFiltering()
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
    
    public static BodyID CreateBody(Shape shape, Vector3 position, Quaternion rotation, ObjectLayer layer, MotionType motionType = MotionType.Static)
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