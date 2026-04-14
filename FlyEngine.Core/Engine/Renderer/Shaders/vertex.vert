#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextureCoord;
layout (location = 2) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 frag_texCoords;
out vec3 frag_worldPos;
out vec3 frag_normal;

void main()
{
    frag_worldPos = vec3(uModel * vec4(aPosition, 1.0));
    frag_normal = normalize(mat3(transpose(inverse(uModel))) * aNormal);
    frag_texCoords = aTextureCoord;
    gl_Position = uProjection * uView * vec4(frag_worldPos, 1.0);
}
