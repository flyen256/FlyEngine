#version 330 core

in vec2 frag_texCoords;
in vec3 frag_worldPos;
in vec3 frag_normal;

uniform sampler2D uTexture;

// uUnlit: 0 = PBR, 1 = unlit (UI multiply by uColor)
uniform float uUnlit;

uniform vec4 uColor;

uniform vec3 uCameraPos;
uniform vec3 uLightPos;
uniform vec3 uLightColor;
uniform vec3 uAmbientColor;

uniform vec3 uAlbedoTint;
uniform float uMetallic;
uniform float uSmoothness;

out vec4 out_color;

const float PI = 3.14159265359;

float distributionGgx(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float nDotH = max(dot(N, H), 0.0);
    float nDotH2 = nDotH * nDotH;
    float denom = nDotH2 * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;
    return a2 / max(denom, 1e-6);
}

float geometrySchlickGgx(float nDotX, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return nDotX / max(nDotX * (1.0 - k) + k, 1e-6);
}

float geometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float nDotV = max(dot(N, V), 0.0);
    float nDotL = max(dot(N, L), 0.0);
    return geometrySchlickGgx(nDotV, roughness) * geometrySchlickGgx(nDotL, roughness);
}

vec3 fresnelSchlick(float cosTheta, vec3 f0)
{
    return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

// Interleaved gradient noise (Dithering to reduce 8-bit banding after gamma)
float interleavedGradientNoise(vec2 pixelCoord)
{
    return fract(52.9829189 * fract(dot(pixelCoord, vec2(0.06711056, 0.00583715))));
}

uniform float uDitherStrength;

void main()
{
    vec4 texSample = texture(uTexture, frag_texCoords);

    if (uUnlit > 0.5)
    {
        out_color = vec4(texSample.rgb * uColor.rgb, texSample.a * uColor.a);
        return;
    }

    float roughness = clamp(1.0 - uSmoothness, 0.04, 1.0);
    vec3 albedo = texSample.rgb * uAlbedoTint;
    float metallic = clamp(uMetallic, 0.0, 1.0);

    vec3 N = normalize(frag_normal);
    vec3 V = normalize(uCameraPos - frag_worldPos);
    vec3 L = normalize(uLightPos - frag_worldPos);
    float dist = max(length(uLightPos - frag_worldPos), 0.05);
    float attenuation = 1.0 / (1.0 + 0.09 * dist + 0.032 * dist * dist);
    vec3 radiance = uLightColor * attenuation;

    vec3 H = normalize(V + L);
    vec3 f0 = mix(vec3(0.04), albedo, metallic);

    float ndf = distributionGgx(N, H, roughness);
    float G = geometrySmith(N, V, L, roughness);
    vec3 F = fresnelSchlick(max(dot(H, V), 0.0), f0);

    float nDotL = max(dot(N, L), 0.0);
    float nDotV = max(dot(N, V), 0.001);
    vec3 specNumer = ndf * G * F;
    float specDenom = 4.0 * nDotV * nDotL;
    vec3 specular = specNumer / max(specDenom, 1e-4);

    vec3 kS = F;
    vec3 kD = (vec3(1.0) - kS) * (1.0 - metallic);
    vec3 diffuse = kD * albedo / PI;

    vec3 Lo = (diffuse + specular) * radiance * nDotL;

    vec3 ambient = uAmbientColor * albedo * (metallic * 0.15 + (1.0 - metallic));
    vec3 color = ambient + Lo;

    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));

    float d = interleavedGradientNoise(floor(gl_FragCoord.xy));
    float amp = (d - 0.5) * (1.0 / 255.0) * clamp(uDitherStrength, 0.0, 4.0);
    color += vec3(amp);

    out_color = vec4(color, texSample.a);
}
