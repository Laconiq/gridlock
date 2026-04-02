#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform vec2 texelSize;

out vec4 finalColor;

void main()
{
    vec4 o = texelSize.xyxy * vec4(-1.0, -1.0, 1.0, 1.0);
    vec4 c = texture(texture0, fragTexCoord) * 4.0;
    c += texture(texture0, fragTexCoord + o.xy);
    c += texture(texture0, fragTexCoord + o.xw);
    c += texture(texture0, fragTexCoord + o.zy);
    c += texture(texture0, fragTexCoord + o.zw);
    finalColor = c * 0.125;
}
