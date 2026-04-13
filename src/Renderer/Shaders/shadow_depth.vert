#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTextureCoord;
layout (location = 2) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uLightMatrix;

void main()
{
    gl_Position = uLightMatrix * uModel * vec4(aPosition, 1.0);
}
