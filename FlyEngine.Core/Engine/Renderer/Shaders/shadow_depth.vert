#version 460 core
#extension GL_ARB_shader_draw_parameters : enable
#extension GL_ARB_bindless_texture : require

layout (location = 0) in vec3 aPosition;

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

uniform mat4 uLightMatrix;

void main()
{
    int drawID = gl_DrawIDARB;

    mat4 model = objects[drawID].modelMatrix;

    gl_Position = uLightMatrix * model * vec4(aPosition, 1.0);
}
