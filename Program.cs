using System.Numerics;
using FlyEngine.Core.Behaviours;
using FlyEngine.Core.Components.Common;
using FlyEngine.Core.Components.Physics;
using FlyEngine.Core.Components.Physics.Colliders;
using FlyEngine.Core.Components.Renderer._3D;
using FlyEngine.Core.Components.Renderer._3D.Meshes;
using FlyEngine.Core.Components.Renderer.Lighting;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.Network;
using JoltPhysicsSharp;

namespace FlyEngine.Core;

public class Program
{
    private static Application? _application;
    private static NetworkManager? _networkManager;

    public static void Main(string[] args)
    {
        TestApplication();
    }

    private static void TestApplication()
    {
        _application = new Application(TestScene);
    }

    private static void TestScene(Application application)
    {
        application.ModelManager.LoadModel("suzanne.fbx");
        application.ModelManager.LoadModel("sphere.fbx");
        
        var camera = Component.CreateGameObject<Camera3D>();
        camera.Transform.Position = new Vector3(0, 1f, 3f);
        camera.AddComponent<CameraController>();

        var cube = Component.CreateGameObject<MeshRenderer>("Cube");
        cube.Mesh = application.Gl.CubeMesh;
        cube.Transform.Size = new Vector3(1.5f, 3f, 1.5f);
        cube.AlbedoTint = new Vector3(0.72f, 0.76f, 0.88f);
        cube.Metallic = 0.12f;
        cube.Smoothness = 0.78f;
        cube.AddComponent<Rigidbody>();
        var cubeCollider = cube.AddComponent<BoxCollider>();
        cubeCollider.HalfExtent = cube.Transform.Size;
        cubeCollider.MotionType = MotionType.Kinematic;

        var ground = Component.CreateGameObject<MeshRenderer>("Ground");
        ground.AddComponent<MeshRenderer>().Mesh = application.Gl.CubeMesh;
        ground.Mesh = application.Gl.CubeMesh;
        ground.Transform.Position = new Vector3(0, -1f, 0);
        ground.Transform.Size = new Vector3(100f, 1f, 100f);
        ground.AlbedoTint = new Vector3(0.2f, 0.24f, 0.19f);
        ground.Metallic = 0f;
        ground.Smoothness = 0.22f;
        ground.AddComponent<Rigidbody>();
        var groundCollider = ground.AddComponent<BoxCollider>();
        groundCollider.HalfExtent = ground.Transform.Size;
        groundCollider.MotionType = MotionType.Kinematic;

        var sunLight = Component.CreateGameObject<LightSource>("Sun");
        sunLight.Transform.Position = new Vector3(0f, 10f, 0f);
        sunLight.Type = LightType.Directional;
        sunLight.Color = new Vector3(1f, 0.96f, 0.88f);
        sunLight.Intensity = 0.34f;
        sunLight.CastShadows = true;
        sunLight.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(0.55f, -0.75f, 0f);

        var pointLight = Component.CreateGameObject<LightSource>();
        pointLight.Transform.Position = new Vector3(10f, 2f, 0f);
        pointLight.Type = LightType.Point;
        pointLight.Color = new Vector3(1f, 0.96f, 0.88f);
        pointLight.Intensity = 1f;
        
        var spotLight = Component.CreateGameObject<LightSource>();
        spotLight.Transform.Position = new Vector3(-10f, 1f, 0f);
        spotLight.Transform.Rotation = QuaternionUtils.FromVector3(new Vector3(-25f, 0f, 0f));
        spotLight.Type = LightType.Spot;
        spotLight.Color = new Vector3(1f, 0.96f, 0.88f);
        spotLight.Intensity = 0.6f;
        spotLight.Range = 10f;

        var suzanne = Component.CreateGameObject<MeshRenderer>();
        suzanne.Transform.Position = new Vector3(0f, 2f, 4);
        suzanne.Transform.Rotation = QuaternionUtils.FromVector3(new Vector3(0f, 0f, 0f));
        suzanne.Mesh = application.ModelManager.TryGetModel("suzanne")?[0];
        
        var physicsSphere = Component.CreateGameObject<MeshRenderer>();
        physicsSphere.Transform.Position = new Vector3(0f, 2f, -4);
        physicsSphere.Mesh = application.ModelManager.TryGetModel("sphere")?[0];
        physicsSphere.AddComponent<Rigidbody>();
        var physicsSphereCollider = physicsSphere.AddComponent<SphereCollider>();
    }

    private static void NetworkTestApplication()
    {
        _application = new Application(NetworkScene);
        _networkManager = new NetworkManager(_application);
    }

    private static void NetworkScene(Application application)
    {
        
    }
}
