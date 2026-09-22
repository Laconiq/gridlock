#version 330

in vec3 fragNormalWS;
in vec3 fragPosWS;

uniform vec4 baseColor;
uniform vec4 emissionColor;
uniform float emissionIntensity;
uniform vec3 lightDir;
uniform vec3 lightColor;

out vec4 finalColor;

void main()
{
    vec3 N = normalize(fragNormalWS);
    vec3 L = normalize(lightDir);

    float NdotL = clamp(dot(N, L), 0.0, 1.0);
    vec3 diffuse = baseColor.rgb * lightColor * NdotL;
    vec3 ambient = baseColor.rgb * 0.15;
    vec3 emission = emissionColor.rgb * emissionIntensity;

    vec3 col = ambient + diffuse + emission;
    finalColor = vec4(col, 1.0);
}
