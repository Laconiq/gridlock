#version 330

// Raylib standard attribute locations
in vec3 vertexPosition;     // location 0
in vec2 vertexTexCoord;     // location 1
in vec3 vertexNormal;       // location 2
in vec4 vertexColor;        // location 3
in vec2 vertexTexCoord2;    // location 5 — barycentric XY

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 fragBary;

void main()
{
    fragBary = vec3(vertexTexCoord2.xy, 1.0 - vertexTexCoord2.x - vertexTexCoord2.y);
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
