#version 330

in vec3 fragBary;

uniform vec4 lineColor;
uniform float emissionIntensity;
uniform float edgeWidth;

out vec4 finalColor;

void main()
{
    vec3 bary = fragBary;
    vec3 fw = fwidth(bary);
    vec3 edge3 = smoothstep(fw * edgeWidth, fw * (edgeWidth + 1.0), bary);
    float edge = min(edge3.x, min(edge3.y, edge3.z));
    float outline = 1.0 - edge;

    vec3 col = lineColor.rgb * emissionIntensity * outline;
    finalColor = vec4(col, outline);
}
