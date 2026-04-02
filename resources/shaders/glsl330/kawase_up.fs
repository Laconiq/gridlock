#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform vec2 texelSize;

out vec4 finalColor;

void main()
{
    vec4 o = texelSize.xyxy * vec4(-1.0, -1.0, 1.0, 1.0);
    vec4 c = vec4(0.0);
    c += texture(texture0, fragTexCoord + vec2(o.x, 0.0));
    c += texture(texture0, fragTexCoord + vec2(o.z, 0.0));
    c += texture(texture0, fragTexCoord + vec2(0.0, o.y));
    c += texture(texture0, fragTexCoord + vec2(0.0, o.w));
    c += texture(texture0, fragTexCoord + o.xy * 0.5) * 2.0;
    c += texture(texture0, fragTexCoord + o.xw * 0.5) * 2.0;
    c += texture(texture0, fragTexCoord + o.zy * 0.5) * 2.0;
    c += texture(texture0, fragTexCoord + o.zw * 0.5) * 2.0;
    finalColor = c / 12.0;
}
