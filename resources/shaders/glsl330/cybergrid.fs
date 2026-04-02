#version 330

in vec3 fragPosWS;
in vec4 fragVertColor;

uniform vec4 gridColor;
uniform float gridSize;
uniform vec2 gridOrigin;
uniform vec2 gridExtent;
uniform float cellFill;
uniform sampler2D cellMap;

out vec4 finalColor;

void main()
{
    // Exact port of Unity CyberGrid.shader fragment
    float gs = max(gridSize, 0.01);
    vec2 uv = fragPosWS.xz / gs;
    vec2 wrapped = abs(fract(uv) - 0.5);
    vec2 duvdx = abs(dFdx(uv));
    vec2 duvdy = abs(dFdy(uv));
    vec2 duv = max(duvdx, duvdy);
    vec2 draw = smoothstep(duv * 0.5, duv * 1.5, wrapped);
    float grid = 1.0 - min(draw.x, draw.y);

    float glow = clamp(dot(fragVertColor.rgb, vec3(0.33, 0.33, 0.33)) * 3.0, 0.0, 1.0);

    vec3 lineColor = mix(gridColor.rgb, fragVertColor.rgb, glow * 0.85);
    float lineBrightness = 0.12 + glow * 0.5;

    vec3 fill = vec3(0.0);
    float fillAlpha = 0.0;
    vec2 cellUV = (fragPosWS.xz - gridOrigin) / gridExtent;

    if (cellUV.x > 0.0 && cellUV.x < 1.0 && cellUV.y > 0.0 && cellUV.y < 1.0)
    {
        vec4 cell = texture(cellMap, cellUV);
        if (cell.a > 0.01)
        {
            lineColor = mix(lineColor, cell.rgb, cell.a * (1.0 - glow));
            vec2 cellLocal = fract(uv);
            float inset = min(min(cellLocal.x, 1.0 - cellLocal.x),
                              min(cellLocal.y, 1.0 - cellLocal.y));
            fill = cell.rgb * cellFill;
            fillAlpha = cellFill * smoothstep(0.05, 0.2, inset) * cell.a;
        }
    }

    float rippleGlow = glow * 0.05;

    vec3 col = lineColor * grid * lineBrightness
             + fill * fillAlpha
             + fragVertColor.rgb * rippleGlow;

    float alpha = clamp(grid * lineBrightness * 8.0 + fillAlpha + rippleGlow, 0.0, 1.0);

    finalColor = vec4(col, alpha);
}
