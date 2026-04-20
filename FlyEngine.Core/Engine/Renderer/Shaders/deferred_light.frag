#version 330 core

#define MAX_LIGHTS 24
#define LT_POINT 0
#define LT_SPOT 1
#define LT_DIR 2
#define LT_AREA 3
#define FOG_STEPS 20

uniform sampler2D uGAlbedoMetallic;
uniform sampler2D uGNormalSmoothness;
uniform sampler2D uDepth;
uniform sampler2D uShadowMap;
uniform samplerCube uSkybox;

uniform vec2 uViewportSize;
uniform mat4 uInvProjection;
uniform mat4 uInverseView;

uniform vec3 uCameraPos;
uniform vec3 uAmbientColor;
uniform vec3 uClearColor;
uniform float uDitherStrength;

uniform int uNumLights;
uniform vec4 uPack0[MAX_LIGHTS];
uniform vec4 uPack1[MAX_LIGHTS];
uniform vec4 uPack2[MAX_LIGHTS];
uniform vec4 uPack3[MAX_LIGHTS];
uniform vec4 uPack4[MAX_LIGHTS];

uniform int uShadowEnabled;
uniform int uShadowDirIndex;
uniform mat4 uLightSpaceMatrix;

uniform vec3 uSunDirWorld;
uniform float uFogDensity;
uniform float uFogHeight;
uniform float uFogFalloff;
uniform float uFogScatter;
uniform vec3 uFogColor;
uniform int uFogEnabled;

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

float interleavedGradientNoise(vec2 pixelCoord)
{
    return fract(52.9829189 * fract(dot(pixelCoord, vec2(0.06711056, 0.00583715))));
}

vec3 brdfLo(vec3 worldPos, vec3 N, vec3 V, vec3 L, vec3 radiance, float roughness, vec3 albedo, float metallic)
{
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

    return (diffuse + specular) * radiance * nDotL;
}

float hash(vec2 p)
{
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float shadowVisibility(vec3 worldPos, vec3 N)
{
    if (uShadowEnabled == 0) return 1.0;

    vec4 ls = uLightSpaceMatrix * vec4(worldPos, 1.0);
    vec3 proj = ls.xyz / ls.w;
    proj = proj * 0.5 + 0.5;

    if (proj.x < 0.0 || proj.x > 1.0 ||
    proj.y < 0.0 || proj.y > 1.0 ||
    proj.z < 0.0 || proj.z > 1.0)
    return 1.0;

    float zReceiver = proj.z;

    vec2 texel = 1.0 / vec2(textureSize(uShadowMap, 0));

    float angle = hash(proj.xy) * 6.2831853;
    mat2 rot = mat2(cos(angle), -sin(angle),
    sin(angle),  cos(angle));

    float shadow = 0.0;
    int radius = 3;

    float cosTheta = clamp(dot(N, uSunDirWorld), 0.0, 1.0);
    float bias = max(0.0008 * (1.0 - cosTheta), 0.00005);

    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -radius; y <= radius; y++)
        {
            vec2 offset = vec2(x, y);
            offset = rot * offset;

            vec2 uv = proj.xy + offset * texel;

            float closest = texture(uShadowMap, uv).r;

            shadow += (zReceiver - bias <= closest) ? 1.0 : 0.0;
        }
    }

    return shadow / float((radius * 2 + 1) * (radius * 2 + 1));
}

vec3 worldRayDir(vec2 uv)
{
    vec2 ndc = uv * 2.0 - 1.0;
    vec4 clipFar = vec4(ndc, 1.0, 1.0);
    vec4 viewFar = uInvProjection * clipFar;
    viewFar /= viewFar.w;
    vec3 w = (uInverseView * vec4(normalize(viewFar.xyz), 0.0)).xyz;
    return normalize(w);
}

float phaseHG(float cosTheta, float g)
{
    float g2 = g * g;
    return (1.0 - g2) / (4.0 * PI * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
}

vec3 applyVolumetricFog(vec3 hdrLinear, vec3 rd, float sceneDist)
{
    if (uFogEnabled == 0) return hdrLinear;

    float dist = min(sceneDist, 120.0);
    if (dist < 0.02) return hdrLinear;

    float stepLen = dist / float(FOG_STEPS);

    vec3 accum = vec3(0.0);
    float trans = 1.0;

    float cosTheta = dot(rd, normalize(uSunDirWorld));
    float phase = phaseHG(cosTheta, 0.6);

    vec3 sunColor = uFogColor * uFogScatter;

    for (int s = 0; s < FOG_STEPS; s++)
    {
        float t = (float(s) + 0.5) * stepLen;
        vec3 sp = uCameraPos + rd * t;

        float heightFactor = exp(-(sp.y - uFogHeight) * uFogFalloff);

        float density = uFogDensity * heightFactor;

        float noise = fract(sin(dot(sp.xz, vec2(12.9898, 78.233))) * 43758.5453);
        density *= mix(0.9, 1.1, noise);

        float absorb = exp(-density * stepLen);

        vec3 scatter = sunColor * phase;

        accum += trans * scatter * density * stepLen;

        trans *= absorb;

        if (trans < 0.01) break;
    }

    return hdrLinear * trans + accum;
}

vec3 evalLight(int i, vec3 worldPos, vec3 N, vec3 V, float roughness, vec3 albedo, float metallic, float dirShadow)
{
    vec4 p0 = uPack0[i];
    vec4 p1 = uPack1[i];
    vec4 p2 = uPack2[i];
    vec4 p3 = uPack3[i];
    vec4 p4 = uPack4[i];

    int type = int(p0.w + 0.5);
    vec3 radianceBase = p1.xyz;
    float range = p1.w;

    if (type == LT_POINT)
    {
        vec3 toL = p0.xyz - worldPos;
        float dist = length(toL);
        if (dist > range || dist < 1e-4) return vec3(0.0);
        vec3 L = toL / dist;
        float att = 1.0 / (1.0 + 0.09 * dist + 0.032 * dist * dist);
        float edge = smoothstep(range * 0.85, range, dist);
        att *= (1.0 - edge);
        vec3 rad = radianceBase * att;
        return brdfLo(worldPos, N, V, L, rad, roughness, albedo, metallic);
    }

    if (type == LT_DIR)
    {
        vec3 L = normalize(-p2.xyz);
        vec3 rad = radianceBase;
        if (uShadowEnabled != 0 && i == uShadowDirIndex)
            rad *= dirShadow;
        return brdfLo(worldPos, N, V, L, rad, roughness, albedo, metallic);
    }

    if (type == LT_SPOT)
    {
        vec3 lightPos = p0.xyz;
        vec3 spotDir = normalize(p2.xyz);
        float cosOuter = p2.w;
        float cosInner = p3.x;

        vec3 toL = lightPos - worldPos;
        float dist = length(toL);
        if (dist < 1e-4) return vec3(0.0);
        vec3 L = toL / dist;
        float att = 1.0 / (dist * dist / range);

        float theta = dot(-L, spotDir);
        float spotMask = smoothstep(cosOuter, cosInner, theta);
        vec3 rad = radianceBase * att * spotMask;
        if (spotMask < 1e-3) return vec3(0.0);
        return brdfLo(worldPos, N, V, L, rad, roughness, albedo, metallic);
    }

    if (type == LT_AREA)
    {
        vec3 C = p0.xyz;
        vec3 n = normalize(p2.xyz);
        vec3 right = normalize(p3.xyz);
        float halfW = p3.w;
        vec3 up = normalize(p4.xyz);
        float halfH = p1.w;

        vec3 co = worldPos - C;
        float x = dot(co, right);
        float y = dot(co, up);
        x = clamp(x, -halfW, halfW);
        y = clamp(y, -halfH, halfH);
        vec3 closest = C + right * x + up * y;
        vec3 toL = closest - worldPos;
        float dist = length(toL);
        if (dist < 1e-4) return vec3(0.0);
        vec3 L = toL / dist;
        float att = 1.0 / max(dist * dist, 0.05);
        att *= (halfW * 2.0) * (halfH * 2.0) / (PI * 4.0 + 1.0);
        vec3 rad = radianceBase * att;
        return brdfLo(worldPos, N, V, L, rad, roughness, albedo, metallic);
    }

    return vec3(0.0);
}

void main()
{
    vec2 uv = gl_FragCoord.xy / uViewportSize;
    float depth = texture(uDepth, uv).r;
    vec3 rd = worldRayDir(uv);

    if (depth >= 0.99995)
    {
        vec3 sky = texture(uSkybox, rd).rgb;
        float sunDot = dot(rd, normalize(uSunDirWorld));
        float sunDisk = smoothstep(0.998, 1.0, sunDot);

        vec3 sunColor = vec3(1.0, 0.9, 0.6) * 30.0;
        sky += sunColor * (sunDisk);

//        float moonDot = dot(rd, -normalize(uSunDirWorld));
//        float moonDisk = smoothstep(0.998, 1.0, moonDot);
//
//        vec3 moonColor = vec3(1.0, 0.9, 0.6) * 30.0;
//        sky += moonColor * (moonDisk);
        out_color = vec4(sky, 1.0);
        return;
    }

    vec4 am = texture(uGAlbedoMetallic, uv);
    vec4 ns = texture(uGNormalSmoothness, uv);
    
    vec3 albedo = am.rgb;
    float metallic = am.a;
    vec3 N = normalize(ns.xyz * 2.0 - 1.0);
    float smoothness = ns.a;
    float roughness = clamp(1.0 - smoothness, 0.04, 1.0);

    vec2 ndc = uv * 2.0 - 1.0;
    float z = depth * 2.0 - 1.0;
    vec4 clip = vec4(ndc, z, 1.0);
    vec4 viewPos = uInvProjection * clip;
    viewPos /= viewPos.w;
    vec4 worldH = uInverseView * vec4(viewPos.xyz, 1.0);
    vec3 worldPos = worldH.xyz;

    vec3 V = normalize(uCameraPos - worldPos);
    float sceneDist = length(worldPos - uCameraPos);

    float dirSh = shadowVisibility(worldPos, N);

    vec3 Lo = vec3(0.0);
    int n = clamp(uNumLights, 0, MAX_LIGHTS);
    for (int i = 0; i < n; i++)
        Lo += evalLight(i, worldPos, N, V, roughness, albedo, metallic, dirSh);

    vec3 ambient = uAmbientColor * albedo * (metallic * 0.15 + (1.0 - metallic));
    vec3 color = ambient + Lo;

    color = applyVolumetricFog(color, rd, sceneDist);

    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));

    float d = interleavedGradientNoise(floor(gl_FragCoord.xy));
    float amp = (d - 0.5) * (1.0 / 255.0) * clamp(uDitherStrength, 0.0, 4.0);
    color += vec3(amp);

    out_color = vec4(color, 1.0);
//    out_color = vec4(N * 0.5 + 0.5, 1.0);
}