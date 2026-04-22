using System.Numerics;
using FlyEngine.Core.Engine.Extensions;
using Silk.NET.Assimp;

namespace FlyEngine.Core.Engine.Renderer.Meshes;

public static class ModelManager
{
    public static readonly Dictionary<string, List<Mesh>> Meshes = [];

    private static readonly Assimp Assimp = Assimp.GetApi();

    public static List<Mesh>? TryGetModel(string name)
    {
        return !Meshes.TryGetValue(name, out var value) ? null : value;
    }

    public static unsafe List<Mesh> LoadModel(string name, OpenGl openGl)
    {
        if (Meshes.ContainsKey(name))
            throw new Exception($"Mesh {name} is already loaded");
        var assembly = typeof(OpenGl).Assembly;
        var names = assembly.GetManifestResourceNames();
        var findName = names.ToList().Find(n => n.Contains(name));
        if (findName == null) return [];
        var stream = assembly.GetManifestResourceMemory(findName);
        if (stream.Length == 0) return [];
        var meshes = new List<Mesh>();
        var ext = name.Split('.').Last();
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

        var meshName = name.Remove(name.Length - 1 - ext.Length, ext.Length + 1);
        Meshes.Add(meshName, meshes);
        
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
        {
            ProcessNode(node->MChildren[i], scene, ref meshes, openGl);
        }
    }

    private static unsafe Mesh? ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, OpenGl openGl)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();
        var textures = new List<Texture>();

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var meshVertex = mesh->MVertices[i];
            var vertex = new MeshVertex
            {
                // BoneIds = new int[Vertex.MAX_BONE_INFLUENCE];
                // Weights = new float[Vertex.MAX_BONE_INFLUENCE];
                Position = meshVertex,
            };

            if (mesh->MNormals != null)
                vertex.Normal = mesh->MNormals[i];
            else
                vertex.Normal = new Vector3(0, 1, 0);
            if (mesh->MTangents != null)
                vertex.Tangent = mesh->MTangents[i];
            if (mesh->MBitangents != null)
                vertex.Bitangent = mesh->MBitangents[i];

            if (mesh->MTextureCoords[0] != null)
            {
                var texcoord3 = mesh->MTextureCoords[0][i];
                vertex.TextureCoordinates = new Vector2(texcoord3.X, texcoord3.Y);
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

        var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse);
        if (diffuseMaps.Count != 0)
            textures.AddRange(diffuseMaps);
        var specularMaps = LoadMaterialTextures(material, TextureType.Specular);
        if (specularMaps.Count != 0)
            textures.AddRange(specularMaps);
        var normalMaps = LoadMaterialTextures(material, TextureType.Height);
        if (normalMaps.Count != 0)
            textures.AddRange(normalMaps);
        var heightMaps = LoadMaterialTextures(material, TextureType.Ambient);
        if (heightMaps.Count != 0)
            textures.AddRange(heightMaps);

        var result = new Mesh(Guid.NewGuid(), openGl.Gl, vertices, indices, (uint)indices.Count);
        return result;
    }
    
    private static unsafe List<Texture> LoadMaterialTextures(Material* mat, TextureType type)
    {
        var textureCount = Assimp.GetMaterialTextureCount(mat, type);
        var textures = new List<Texture>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            Assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            var skip = false;
            // new Texture(path, Application.Instance.Gl);
            // for (int j = 0; j < _texturesLoaded.Count; j++)
            // {
            //     if (_texturesLoaded[j].Path == path)
            //     {
            //         textures.Add(_texturesLoaded[j]);
            //         skip = true;
            //         break;
            //     }
            // }
            // if (skip) continue;
            // var texture = new Texture(_gl, Directory, type);
            // texture.Path = path;
            // textures.Add(texture);
            // _texturesLoaded.Add(texture);
        }
        return textures;
    }
}