#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform float threshold;

out vec4 finalColor;

void main()
{
    finalColor = max(texture(texture0, fragTexCoord) - vec4(threshold), 0.0);
}
