#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec2 vertexTexCoord;
layout(location = 2) in vec3 vertexNormal;
layout(location = 3) in vec4 vertexColor;

uniform mat4 matView;
uniform mat4 matProjection;

out vec3 fragNormalWS;
out vec4 fragVertColor;
out vec3 fragPosWS;

// Per-instance data packed into vertexColor + vertexTexCoord2
// For DrawMeshInstanced, Raylib passes instance transforms via
// a mat4 composed from the instance data. We reconstruct from
// the model matrix provided per-instance.
uniform mat4 matModel;

void main()
{
    vec4 worldPos = matModel * vec4(vertexPosition, 1.0);
    fragPosWS = worldPos.xyz;
    fragNormalWS = mat3(transpose(inverse(matModel))) * vertexNormal;
    fragVertColor = vertexColor;
    gl_Position = matProjection * matView * worldPos;
}
