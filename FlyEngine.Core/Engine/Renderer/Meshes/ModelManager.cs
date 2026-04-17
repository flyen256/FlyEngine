using System.Numerics;
using FlyEngine.Core.Engine.Extensions;
using Silk.NET.Assimp;

namespace FlyEngine.Core.Engine.Renderer.Meshes;

public class ModelManager
{
    public readonly Dictionary<string, List<Mesh>> Meshes = [];

    private Assimp _assimp = Assimp.GetApi();

    public List<Mesh>? TryGetModel(string name)
    {
        return !Meshes.TryGetValue(name, out var value) ? null : value;
    }

    public unsafe List<Mesh> LoadModel(string name)
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
                var scene = _assimp.ImportFileFromMemory(pData, (uint)stream.Length,
                    (uint)(PostProcessSteps.Triangulate |
                           PostProcessSteps.GenerateNormals |
                           PostProcessSteps.JoinIdenticalVertices), pHint);
                if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
                {
                    var error = _assimp.GetErrorStringS();
                    throw new Exception(error);
                }
                ProcessNode(scene->MRootNode, scene, ref meshes);
            }
        }

        var meshName = name.Remove(name.Length - 1 - ext.Length, ext.Length + 1);
        Console.WriteLine(meshName);
        Meshes.Add(meshName, meshes);
        
        return meshes;
    }

    private unsafe void ProcessNode(Node* node, Scene* scene, ref List<Mesh> meshes)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            meshes.Add(ProcessMesh(mesh, scene));
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene, ref meshes);
        }
    }

    private unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene)
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
        // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
        // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
        // Same applies to other texture as the following list summarizes:
        // diffuse: texture_diffuseN
        // specular: texture_specularN
        // normal: texture_normalN

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

        var result = new Mesh(Application.Instance.Gl.Gl, vertices, indices, (uint)indices.Count);
        return result;
    }
    
    private unsafe List<Texture> LoadMaterialTextures(Material* mat, TextureType type)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        List<Texture> textures = new List<Texture>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            var skip = false;
            new Texture(path, Application.Instance.Gl);
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