using System.Numerics;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.Renderer;
using Silk.NET.Assimp;
using File = System.IO.File;

namespace FlyEngine.Core.Assets;

public static class ModelManager
{
    public static readonly Assimp Assimp = Assimp.GetApi();

    public static Model LoadModel(OpenGl openGl, string path, string name)
    {
        if (AssetsManager.LoadedAssets.Contains(path))
            throw new Exception($"Model {path} is already loaded");
        AssetsManager.TryLoadAssetGlobal(path, out Model? model);
        var meshes = LoadModelMeshes(openGl, path);
        for (var i = 0; i < meshes.Count; i++)
        {
            if (model == null) break;
            if (i >= model.MeshesGuids.Count) break;
            meshes[i].Guid = model.MeshesGuids[i];
        }
        return new Model(model?.Guid ?? Guid.NewGuid(), path, name, meshes);
    }

    private static unsafe List<Mesh> LoadModelMeshes(OpenGl openGl, string path)
    {
        if (AssetsManager.LoadedAssets.Contains(path))
            throw new Exception($"Model {path} is already loaded");
        var streamBytes = File.ReadAllBytes(path);
        if (streamBytes.Length == 0) return [];
        
        var meshes = new List<Mesh>();
        var ext = path.Split('.').Last();
        var hintBytes = System.Text.Encoding.ASCII.GetBytes(ext + "\0");
        fixed (byte* pData = streamBytes)
        {
            fixed (byte* pHint = hintBytes)
            {
                var scene = Assimp.ImportFileFromMemory(pData, (uint)streamBytes.Length,
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
    
    public static unsafe List<Mesh> LoadModelMeshesFromAssembly(OpenGl openGl, string name)
    {
        var assembly = typeof(OpenGl).Assembly;
        var names = assembly.GetManifestResourceNames();
        
        var findName = names.ToList().Find(s => s.Contains("sphere.fbx"));
        if (findName == null) return [];
        
        var stream = assembly.GetManifestResourceStream(findName);
        if (stream == null) return [];
        
        var streamBytes = stream.StreamToByteArray();
        if (streamBytes.Length == 0) return [];
        
        var meshes = new List<Mesh>();
        var ext = name.Split('.').Last();
        var hintBytes = System.Text.Encoding.ASCII.GetBytes(ext + "\0");
        fixed (byte* pData = streamBytes)
        {
            fixed (byte* pHint = hintBytes)
            {
                var scene = Assimp.ImportFileFromMemory(pData, (uint)streamBytes.Length,
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
            meshes.Add(ProcessMesh(mesh, scene, openGl));
        }

        for (var i = 0; i < node->MNumChildren; i++)
            ProcessNode(node->MChildren[i], scene, ref meshes, openGl);
    }

    private static unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, OpenGl openGl)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var meshVertex = mesh->MVertices[i];
            var vertex = new MeshVertex
            {
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

        return new Mesh(Guid.NewGuid(), vertices, indices, (uint)indices.Count)
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