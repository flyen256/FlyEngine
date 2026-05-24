#version 330 core

out vec4 FragColor;

uniform sampler2D gDepth;
uniform vec2 viewportSize;
uniform float uIsSelected;

float getDepth(vec2 uv) {
    return texture(gDepth, uv).r;
}

void main() {
    vec2 uv = gl_FragCoord.xy / viewportSize;
    float depth = getDepth(uv);

    float offset = 1.0 / viewportSize.x;
    float dLeft  = getDepth(uv + vec2(-offset, 0));
    float dRight = getDepth(uv + vec2(offset, 0));
    float dUp    = getDepth(uv + vec2(0, offset));
    float dDown  = getDepth(uv + vec2(0, -offset));

    float edge = abs(depth - dLeft) + abs(depth - dRight) + abs(depth - dUp) + abs(depth - dDown);

    if (edge > 0.00001 && uIsSelected == 1) {
        FragColor = vec4(1.0, 0.5, 0.0, 1.0);
    } else {
        discard;
    }
}