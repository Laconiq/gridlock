#version 330

// Diagnostic shader: bright cyan grid lines, no cell map or glow effects.
// If this renders correctly but the full shader does not, the issue is in
// the shader math (cell map sampling, vertex color glow, etc.).
// If this also fails, the issue is in the pipeline (mesh data, uniforms,
// render state, or compositing).

in vec3 fragPosWS;
in vec4 fragVertColor;

uniform float gridSize;

out vec4 finalColor;

void main()
{
    // Guard against gridSize being zero (uniform not set)
    float gs = max(gridSize, 0.01);

    vec2 uv = fragPosWS.xz / gs;
    vec2 wrapped = abs(fract(uv) - 0.5);
    vec2 fw = fwidth(uv);

    vec2 draw = smoothstep(fw * 0.5, fw * 2.0, wrapped);
    float grid = 1.0 - min(draw.x, draw.y);

    // Bright cyan on grid lines, fully transparent between them
    finalColor = vec4(0.0, grid, grid, grid);
}
