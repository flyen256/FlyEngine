#version 330 core

in vec2 frag_texCoords;
uniform sampler2D uTexture;
uniform vec4 uColor;

out vec4 out_color;

void main()
{
    out_color = texture(uTexture, frag_texCoords) * uColor;
}
