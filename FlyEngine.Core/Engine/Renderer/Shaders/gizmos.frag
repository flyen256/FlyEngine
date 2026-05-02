#version 330 core

out vec4 FragColor;

uniform vec4 uGizmoColor;

void main()
{
    FragColor = uGizmoColor;
}