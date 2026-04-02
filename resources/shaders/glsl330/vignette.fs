#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform float intensity;
uniform vec3 vignetteColor;

out vec4 finalColor;

void main()
{
    vec4 col = texture(texture0, fragTexCoord);
    vec2 center = vec2(0.5);
    float dist = length(fragTexCoord - center) * 1.414;
    float vignette = 1.0 - clamp(dist * dist * intensity, 0.0, 1.0);

    col.rgb = mix(vignetteColor, col.rgb, vignette);
    finalColor = col;
}
