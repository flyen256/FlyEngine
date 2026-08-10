#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D uLightedTexture;
uniform sampler2D uDepthTexture;

uniform vec3 uCameraPos;
uniform mat4 uInvProjView;
uniform vec3 uFogColor;
uniform float uFogDensity;

vec3 worldPosFromDepth(float depth, vec2 uv) {
    vec4 ndc = vec4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    vec4 wp = uInvProjView * ndc;
    return wp.xyz / wp.w;
}

void main() {
    vec4 sceneColor = texture(uLightedTexture, TexCoords);
    float depth = texture(uDepthTexture, TexCoords).r;

    float fogFactor;

    // Для неба (depth = 1.0)
    if (depth >= 0.9999) {
        fogFactor = 0.3; // 70% тумана для неба
    } else {
        // Получаем мировую позицию
        vec3 worldPos = worldPosFromDepth(depth, TexCoords);

        // Расстояние от камеры
        float distance = length(worldPos - uCameraPos);

        // Простой экспоненциальный туман
        fogFactor = exp(-uFogDensity * distance);
    }

    // Смешиваем цвет сцены с цветом тумана
    vec3 finalColor = mix(uFogColor, sceneColor.rgb, fogFactor);

    FragColor = vec4(finalColor, sceneColor.a);
}