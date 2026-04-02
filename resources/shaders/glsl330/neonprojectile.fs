#version 330

in vec3 fragNormalWS;
in vec3 fragViewDirWS;
in vec3 fragPosOS;

uniform vec4 color0;
uniform vec4 color1;
uniform vec4 color2;
uniform vec4 color3;
uniform float colorCount;
uniform float emissionIntensity;
uniform float rimPower;
uniform float rimWidth;
uniform float rimIntensity;
uniform float pulseSpeed;
uniform float pulseMin;
uniform float time;

out vec4 finalColor;

vec3 blendElementColors(float t)
{
    int count = int(colorCount);
    if (count <= 1) return color0.rgb;

    vec4 colors[4] = vec4[4](color0, color1, color2, color3);
    float scaled = t * float(count);
    int idx = int(scaled) % count;
    int next = (idx + 1) % count;
    return mix(colors[idx].rgb, colors[next].rgb, fract(scaled));
}

void main()
{
    vec3 N = normalize(fragNormalWS);
    vec3 V = normalize(fragViewDirWS);
    float NdotV = clamp(dot(N, V), 0.0, 1.0);

    float rim = 1.0 - NdotV;
    float rimMask = smoothstep(1.0 - rimWidth, 1.0, rim);
    rimMask = pow(rimMask, rimPower);

    vec3 posNorm = normalize(fragPosOS);
    float colorT = dot(posNorm, vec3(0.5, 0.7, 0.3)) * 0.5 + 0.5;
    colorT += time * 0.12;
    vec3 elemColor = blendElementColors(colorT);

    float pulse = 1.0;
    if (pulseSpeed > 0.001)
        pulse = mix(pulseMin, 1.0, sin(time * pulseSpeed) * 0.5 + 0.5);

    vec3 col = elemColor * rimMask * emissionIntensity * rimIntensity * pulse;
    finalColor = vec4(col, 1.0);
}
