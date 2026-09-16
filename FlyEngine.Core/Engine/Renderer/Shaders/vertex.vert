#version 460 core

#extension GL_ARB_bindless_texture : require

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextureCoord;
layout (location = 2) in vec3 aNormal;

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

uniform mat4 uView;
uniform mat4 uProjection;

out vec2 frag_texCoords;
out vec3 frag_worldPos;
out vec3 frag_normal;
flat out int DrawID;

void main()
{
    DrawID = gl_DrawID;

    mat4 model = objects[gl_DrawID].modelMatrix;

    vec4 worldPos = model * vec4(aPosition, 1.0);
    frag_worldPos = worldPos.xyz;

    frag_normal = normalize(mat3(transpose(inverse(model))) * aNormal);

    frag_texCoords = aTextureCoord;

    gl_Position = uProjection * uView * worldPos;
}
