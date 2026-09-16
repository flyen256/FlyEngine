using System.Numerics;
using FlyEngine.Core.Extensions;
using FlyEngine.Core.Renderer;
using Silk.NET.Assimp;
using StbImageSharp;
using File = System.IO.File;

namespace FlyEngine.Core.Assets;

public static class ModelManager
{
    public static readonly Assimp Assimp = Assimp.GetApi();

    private const uint ImportFlags = (uint)(PostProcessSteps.Triangulate | 
                                            PostProcessSteps.GenerateNormals | 
                                            PostProcessSteps.JoinIdenticalVertices);

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

    private static List<Mesh> LoadModelMeshes(OpenGl openGl, string path)
    {
        var streamBytes = File.ReadAllBytes(path);
        if (streamBytes.Length == 0) return [];
        
        var ext = path.Split('.').Last();
        var hintBytes = System.Text.Encoding.ASCII.GetBytes(ext + "\0");
        
        return ImportFile(streamBytes, hintBytes, openGl, path);
    }
    
    public static List<Mesh> LoadModelMeshesFromAssembly(OpenGl openGl, string name)
    {
        var assembly = typeof(OpenGl).Assembly;
        var names = assembly.GetManifestResourceNames();
        
        var findName = names.ToList().Find(s => s.Contains(name));
        if (findName == null) return [];
        
        var stream = assembly.GetManifestResourceStream(findName);
        if (stream == null) return [];
        
        var streamBytes = stream.StreamToByteArray();
        if (streamBytes.Length == 0) return [];
        
        var ext = name.Split('.').Last();
        var hintBytes = System.Text.Encoding.ASCII.GetBytes(ext + "\0");
        
        return ImportFile(streamBytes, hintBytes, openGl, string.Empty);
    }

    private static unsafe List<Mesh> ImportFile(byte[] streamBytes, byte[] hintBytes, OpenGl openGl, string modelPath)
    {
        var meshes = new List<Mesh>();
        fixed (byte* pData = streamBytes)
        fixed (byte* pHint = hintBytes)
        {
            var scene = Assimp.ImportFileFromMemory(pData, (uint)streamBytes.Length, ImportFlags, pHint);
            if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
            {
                var error = Assimp.GetErrorStringS();
                throw new Exception(error);
            }
            var blenderScaleMatrix = Matrix4x4.CreateScale(0.01f);
            ProcessNode(scene->MRootNode, scene, ref meshes, openGl, modelPath, blenderScaleMatrix);
        }
        return meshes;
    }

    private static unsafe void ProcessNode(Node* node, Scene* scene, ref List<Mesh> meshes, OpenGl openGl, string modelPath, Matrix4x4 parentTransform)
    {
        var nodeLocalTransform = ConvertAssimpMatrix(node->MTransformation);
        var currentTransform = nodeLocalTransform * parentTransform;
        
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            meshes.Add(ProcessMesh(mesh, scene, openGl, modelPath, currentTransform));
        }

        for (var i = 0; i < node->MNumChildren; i++)
            ProcessNode(node->MChildren[i], scene, ref meshes, openGl, modelPath, currentTransform);
    }

    private static unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene, OpenGl openGl, string modelPath, Matrix4x4 transform)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            var position = mesh->MVertices[i];
            var normal = mesh->MNormals != null ? mesh->MNormals[i] : Vector3.UnitY;
            var tangent = mesh->MTangents != null ? mesh->MTangents[i] : Vector3.Zero;
            var bitangent = mesh->MBitangents != null ? mesh->MBitangents[i] : Vector3.Zero;

            position = Vector3.Transform(position, transform);
            normal = Vector3.Normalize(Vector3.TransformNormal(normal, transform));
            if (mesh->MTangents != null) tangent = Vector3.Normalize(Vector3.TransformNormal(tangent, transform));
            if (mesh->MBitangents != null) bitangent = Vector3.Normalize(Vector3.TransformNormal(bitangent, transform));

            var vertex = new MeshVertex
            {
                Position = position,
                Normal = normal,
                Tangent = tangent,
                Bitangent = bitangent
            };

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

        LoadMaterialTextures(scene, material, textures, Silk.NET.Assimp.TextureType.Diffuse, openGl, modelPath);
        LoadMaterialTextures(scene, material, textures, Silk.NET.Assimp.TextureType.Specular, openGl, modelPath);
        LoadMaterialTextures(scene, material, textures, Silk.NET.Assimp.TextureType.Height, openGl, modelPath);
        LoadMaterialTextures(scene, material, textures, Silk.NET.Assimp.TextureType.Ambient, openGl, modelPath);

        return new Mesh(Guid.NewGuid(), mesh->MName, textures, vertices, indices, (uint)indices.Count);
    }
    
    private static unsafe void LoadMaterialTextures(
        Scene* scene, Silk.NET.Assimp.Material* mat,
        List<Texture> textures,
        Silk.NET.Assimp.TextureType type,
        OpenGl openGl,
        string modelPath)
    {
        var textureCount = Assimp.GetMaterialTextureCount(mat, type);
        var loadedTextures = AssetsManager.GetAssets<Texture>();

        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            Assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            
            var relativePath = path.ToString();

            var skip = false;
            for (var j = 0; j < loadedTextures.Count; j++)
            {
                var currentTexture = loadedTextures[j];
                if (currentTexture.AssimpPath != relativePath) continue;
                
                textures.Add(currentTexture);
                skip = true;
                break;
            }
            if (skip) continue;

            byte[] textureData;
            uint width;
            uint height;

            Silk.NET.Assimp.Texture* embeddedTexture = null;
            for (uint t = 0; t < scene->MNumTextures; t++)
            {
                var embeddedName = scene->MTextures[t]->MFilename.ToString();
                if (embeddedName != relativePath && relativePath != $"*{t}") continue;
                embeddedTexture = scene->MTextures[t];
                break;
            }

            if (embeddedTexture != null)
            {
                if (embeddedTexture->MHeight == 0)
                {
                    var dataSize = embeddedTexture->MWidth; 
                    textureData = new byte[dataSize];
                    System.Runtime.InteropServices.Marshal.Copy((IntPtr)embeddedTexture->PcData, textureData, 0, (int)dataSize);

                    using var stream = new MemoryStream(textureData);
                    
                    var info = ImageInfo.FromStream(stream);
                    width = info.HasValue ? (uint)info.Value.Width : 0;
                    height = info.HasValue ? (uint)info.Value.Height : 0;
                }
                else
                {
                    width = embeddedTexture->MWidth;
                    height = embeddedTexture->MHeight;
                    var byteSize = width * height * 4;
                    textureData = new byte[byteSize];
                    System.Runtime.InteropServices.Marshal.Copy((IntPtr)embeddedTexture->PcData, textureData, 0, (int)byteSize);
                }
            }
            else
            {
                var modelDirectory = Path.GetDirectoryName(modelPath) ?? string.Empty;
                var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                var fileNameOnly = Path.GetFileName(normalizedPath); 
                
                var fullPath = Path.Combine(modelDirectory, normalizedPath);
                var alternativePath = Path.Combine(modelDirectory, fileNameOnly);

                var finalPath = File.Exists(fullPath) ? fullPath : (File.Exists(alternativePath) ? alternativePath : null);

                if (finalPath != null)
                {
                    textureData = File.ReadAllBytes(finalPath);
                    
                    using var stream = new MemoryStream(textureData);
                    
                    var info = ImageInfo.FromStream(stream);
                    width = info.HasValue ? (uint)info.Value.Width : 0;
                    height = info.HasValue ? (uint)info.Value.Height : 0;
                }
                else
                {
                    Console.WriteLine($"[Error] Texture not found anywhere: {relativePath}");
                    continue;
                }
            }

            var texture = new Texture(Guid.NewGuid(), textureData, width, height, openGl, relativePath);
            textures.Add(texture);
        }
    }
    
    private static Matrix4x4 ConvertAssimpMatrix(Matrix4x4 aiMat)
    {
        return new Matrix4x4(
            aiMat.M11, aiMat.M12, aiMat.M13, aiMat.M14,
            aiMat.M21, aiMat.M22, aiMat.M23, aiMat.M24,
            aiMat.M31, aiMat.M32, aiMat.M33, aiMat.M34,
            aiMat.M41, aiMat.M42, aiMat.M43, aiMat.M44
        );
    }
}