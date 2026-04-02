#version 330

in vec2 fragTexCoord;

uniform sampler2D texture0;
uniform vec2 resolution;
uniform float chromaticIntensity;
uniform float vignetteIntensity;
uniform vec3 vignetteColor;

out vec4 finalColor;

void main()
{
    vec2 center = vec2(0.5);
    vec2 dir = fragTexCoord - center;
    float dist = length(dir);

    // --- Chromatic aberration ---
    vec4 col;
    if (chromaticIntensity > 0.001)
    {
        vec2 offset = dir * dist * chromaticIntensity;
        float r = texture(texture0, fragTexCoord + offset).r;
        float g = texture(texture0, fragTexCoord).g;
        float b = texture(texture0, fragTexCoord - offset).b;
        col = vec4(r, g, b, 1.0);
    }
    else
    {
        col = texture(texture0, fragTexCoord);
    }

    // --- Vignette ---
    float vDist = dist * 1.414;
    float vignette = 1.0 - clamp(vDist * vDist * vignetteIntensity, 0.0, 1.0);
    col.rgb = mix(vignetteColor, col.rgb, vignette);

    // --- Pixel grid + scanline ---
    vec2 pixelUV = fragTexCoord * resolution;
    vec2 grid = abs(fract(pixelUV) - 0.5) * 2.0;
    float gridLine = max(grid.x, grid.y);
    float gridDarken = mix(1.0, 0.85, smoothstep(0.8, 1.0, gridLine));
    float scanline = 0.95 + 0.05 * sin(fragTexCoord.y * resolution.y * 3.14159);

    col.rgb *= gridDarken * scanline;

    finalColor = col;
}
