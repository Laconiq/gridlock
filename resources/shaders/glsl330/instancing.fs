#version 330

in vec3 fragNormalWS;
in vec4 fragVertColor;
in vec3 fragPosWS;

uniform vec4 colDiffuse;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform float emissionIntensity;

out vec4 finalColor;

void main()
{
    vec3 N = normalize(fragNormalWS);
    vec3 L = normalize(lightDir);

    float NdotL = clamp(dot(N, L), 0.0, 1.0);
    vec3 baseCol = colDiffuse.rgb * fragVertColor.rgb;
    vec3 diffuse = baseCol * lightColor * NdotL;
    vec3 ambient = baseCol * 0.15;
    vec3 emission = baseCol * emissionIntensity;

    vec3 col = ambient + diffuse + emission;
    float alpha = colDiffuse.a * fragVertColor.a;

    finalColor = vec4(col, alpha);
}
