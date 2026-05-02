using System.Numerics;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.Renderer;
using Silk.NET.Assimp;
using File = System.IO.File;

namespace FlyEngine.Core.Assets;

public static class ModelManager
{
    public static readonly Assimp Assimp = Assimp.GetApi();
    
    public static unsafe List<Mesh> LoadModel(OpenGl openGl, string path)
    {
        if (AssetsManager.LoadedAssetsPaths.Contains(path))
            throw new Exception($"Mesh {path} is already loaded");
        var stream = File.ReadAllBytes(path);
        if (stream.Length == 0) return [];
        var meshes = new List<Mesh>();
        var ext = path.Split('.').Last();
        var hintBytes = System.Text.Encoding.ASCII.GetBytes(ext + "\0");
        fixed (byte* pData = stream)
        {
            fixed (byte* pHint = hintBytes)
            {
                var scene = Assimp.ImportFileFromMemory(pData, (uint)stream.Length,
                    (uint)(PostProcessSteps.Triangulate |
                           PostProcessSteps.GenerateNormals |
                           PostProcessSteps.JoinIdenticalVertices), pHint);
                if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
                {
                    var error = Assimp.GetErrorStringS();
                    throw new Exception(error);
                }
                ProcessNode(scene->MRootNode, scene, ref meshes, openGl);
            }
        }
        
        return meshes;
    }

    public static unsafe List<Mesh> LoadModel(string embeddedResourceName, OpenGl openGl)
    {
        if (AssetsManager.LoadedAssetsPaths.Contains(embeddedResourceName))
            throw new Exception($"Mesh {embeddedResourceName} is already loaded");
        var assembly = typeof(OpenGl).Assembly;
        var name = assembly.GetManifestResourceNames().ToList().Find(n => n.Contains(embeddedResourceName));
        if (name == null) return [];
        var stream = assembly.GetManifestResourceMemory(name);
        if (stream.Length == 0) return [];
        var meshes = new List<Mesh>();
        var hintBytes = System.Text.Encoding.ASCII.GetBytes(embeddedResourceName.Split('.').Last() + "\0");
        fixed (byte* pData = stream)
        {
            fixed (byte* pHint = hintBytes)
            {
                var scene = Assimp.ImportFileFromMemory(pData, (uint)stream.Length,
                    (uint)(PostProcessSteps.Triangulate |
                           PostProcessSteps.GenerateNormals |
                           PostProcessSteps.JoinIdenticalVertices), pHint);
                if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
                {
                    var error = Assimp.GetErrorStringS();
                    throw new Exception(error);
                }
                ProcessNode(scene->MRootNode, scene, ref meshes, openGl);
            }
        }
        
        return meshes;
    }

    private static unsafe void ProcessNode(Node* node, Scene* scene, ref List<Mesh> meshes, OpenGl openGl)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            var processedMesh = ProcessMesh(mesh, scene, openGl);
            if (processedMesh != null)
                meshes.Add(processedMesh);
        }

        for (var i = 0; i < node->MNumChildren; i++)
            ProcessNode(node->MChildren[i], scene, ref meshes, openGl);
    }

    private static unsafe Mesh? ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, OpenGl openGl)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var meshVertex = mesh->MVertices[i];
            var vertex = new MeshVertex
            {
                // BoneIds = new int[Vertex.MAX_BONE_INFLUENCE];
                // Weights = new float[Vertex.MAX_BONE_INFLUENCE];
                Position = meshVertex,
                Normal = mesh->MNormals != null ? mesh->MNormals[i] : new Vector3(0, 1, 0)
            };
            if (mesh->MTangents != null)
                vertex.Tangent = mesh->MTangents[i];
            if (mesh->MBitangents != null)
                vertex.Bitangent = mesh->MBitangents[i];

            if (mesh->MTextureCoords[0] != null)
            {
                var textureCoords = mesh->MTextureCoords[0][i];
                vertex.TextureCoordinates = new Vector2(textureCoords.X, textureCoords.Y);
            }

            vertices.Add(vertex);
        }

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }

        var material = scene->MMaterials[mesh->MMaterialIndex];
        var textures = new List<Texture>();

        LoadMaterialTextures(material, textures, TextureType.Diffuse, openGl);
        LoadMaterialTextures(material, textures, TextureType.Specular, openGl);
        LoadMaterialTextures(material, textures, TextureType.Height, openGl);
        LoadMaterialTextures(material, textures, TextureType.Ambient, openGl);

        return new Mesh(Guid.NewGuid(), textures, vertices, indices, (uint)indices.Count)
        {
            Name = mesh->MName
        };
    }
    
    private static unsafe void LoadMaterialTextures(Material* mat, List<Texture> textures, TextureType type, OpenGl openGl)
    {
        var textureCount = Assimp.GetMaterialTextureCount(mat, type);
        var loadedTextures = AssetsManager.GetAssets<Texture>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            Assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            var skip = false;
            for (var j = 0; j < loadedTextures.Count; j++)
            {
                var currentTexture = loadedTextures[j];
                if (currentTexture.AssimpPath != path) continue;
                textures.Add(currentTexture);
                skip = true;
                break;
            }
            if (skip) continue;
            var texture = new Texture(Guid.NewGuid(), type, path, openGl);
            textures.Add(texture);
            AssetsManager.AddAsset(texture);
        }
    }
}