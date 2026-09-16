#version 460 core

#extension GL_ARB_bindless_texture : require
#extension GL_ARB_gpu_shader_int64 : enable

layout (location = 0) out vec4 gAlbedoMetallic;
layout (location = 1) out vec4 gNormalSmoothness;

in vec2 frag_texCoords;
in vec3 frag_worldPos;
in vec3 frag_normal;
flat in int DrawID;

struct ObjectMaterialData {
    mat4 modelMatrix;
    vec4 albedoTint;
    sampler2D albedo;
    float metallic;
    float smoothness;
    float padding1;
    float padding2;
};

layout(std430, binding = 0) buffer MaterialBuffer {
    ObjectMaterialData objects[];
};

uniform sampler2D uTexture;

void main()
{
    ObjectMaterialData object = objects[DrawID];
    vec3 albedoTint = object.albedoTint.xyz;
    float metallic  = object.metallic;
    float smoothness = object.smoothness;

    vec4 tex = texture(object.albedo, frag_texCoords);
    vec3 albedo = tex.rgb * albedoTint;

    vec3 n = normalize(frag_normal);

    gAlbedoMetallic   = vec4(albedo, clamp(metallic, 0.0, 1.0));
    gNormalSmoothness = vec4(n * 0.5 + 0.5, clamp(smoothness, 0.0, 1.0));
}
