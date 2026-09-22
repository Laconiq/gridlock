#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform float emissionIntensity;

out vec4 finalColor;

void main()
{
    vec3 col = fragColor.rgb * emissionIntensity;
    float alpha = fragColor.a;

    float widthFade = smoothstep(0.0, 0.2, fragTexCoord.y) *
                      smoothstep(0.0, 0.2, 1.0 - fragTexCoord.y);
    alpha *= widthFade;

    finalColor = vec4(col, alpha);
}
