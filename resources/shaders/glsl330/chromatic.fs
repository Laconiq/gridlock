#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform float intensity;

out vec4 finalColor;

void main()
{
    vec2 center = vec2(0.5);
    vec2 dir = fragTexCoord - center;
    float dist = length(dir);
    vec2 offset = dir * dist * intensity;

    float r = texture(texture0, fragTexCoord + offset).r;
    float g = texture(texture0, fragTexCoord).g;
    float b = texture(texture0, fragTexCoord - offset).b;
    float a = texture(texture0, fragTexCoord).a;

    finalColor = vec4(r, g, b, a);
}
