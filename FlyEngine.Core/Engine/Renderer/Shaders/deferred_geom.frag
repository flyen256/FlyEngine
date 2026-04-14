#version 330 core

layout (location = 0) out vec4 gAlbedoMetallic;
layout (location = 1) out vec4 gNormalSmoothness;

in vec2 frag_texCoords;
in vec3 frag_worldPos;
in vec3 frag_normal;

uniform sampler2D uTexture;
uniform vec3 uAlbedoTint;
uniform float uMetallic;
uniform float uSmoothness;

void main()
{
    vec4 tex = texture(uTexture, frag_texCoords);
    vec3 albedo = tex.rgb * uAlbedoTint;
    vec3 n = normalize(frag_normal);
    gAlbedoMetallic = vec4(albedo, clamp(uMetallic, 0.0, 1.0));
    gNormalSmoothness = vec4(n * 0.5 + 0.5, clamp(uSmoothness, 0.0, 1.0));
}
