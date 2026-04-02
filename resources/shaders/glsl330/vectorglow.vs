#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec2 vertexTexCoord;
layout(location = 2) in vec3 vertexNormal;

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 fragNormalWS;
out vec3 fragPosWS;

void main()
{
    fragPosWS = (matModel * vec4(vertexPosition, 1.0)).xyz;
    fragNormalWS = mat3(transpose(inverse(matModel))) * vertexNormal;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
