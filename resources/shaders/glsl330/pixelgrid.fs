#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform vec2 resolution;

out vec4 finalColor;

void main()
{
    vec4 col = texture(texture0, fragTexCoord);

    vec2 pixelUV = fragTexCoord * resolution;
    vec2 grid = abs(fract(pixelUV) - 0.5) * 2.0;
    float gridLine = max(grid.x, grid.y);
    float gridDarken = mix(1.0, 0.85, smoothstep(0.8, 1.0, gridLine));

    float scanline = 0.95 + 0.05 * sin(fragTexCoord.y * resolution.y * 3.14159);

    col.rgb *= gridDarken * scanline;

    finalColor = col;
}
