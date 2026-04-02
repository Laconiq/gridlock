#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec2 vertexTexCoord;
layout(location = 2) in vec3 vertexNormal;
layout(location = 3) in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 fragPosWS;
out vec4 fragVertColor;

void main()
{
    // matModel is identity when drawn with Matrix4x4.Identity, so
    // fragPosWS equals the deformed vertex position in world space.
    // Guard against a zero matrix (uniform never set) by checking the
    // bottom-right element that would be 1.0 in any valid transform.
    if (matModel[3][3] > 0.5)
        fragPosWS = (matModel * vec4(vertexPosition, 1.0)).xyz;
    else
        fragPosWS = vertexPosition;

    fragVertColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
