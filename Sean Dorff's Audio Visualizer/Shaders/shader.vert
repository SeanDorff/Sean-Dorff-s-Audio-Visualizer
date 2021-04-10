#version 460
#define PRIMITIVE_POINT 0
#define PRIMITIVE_TRIANGLE 1

layout (location = 0) in vec4 aPosition;
layout (location = 1) in vec4 aColor;
out vec4 vertexColor;
out float distanceToCamera;

uniform mat4 modelViewProjection;
uniform float drift;
uniform vec3 cameraPosition;
uniform int primitiveType;

void main(void)
{
    vec3 driftedPosition = vec3(aPosition.xy, aPosition.z - aPosition.w * drift);
    distanceToCamera = abs(distance(driftedPosition, cameraPosition));
    gl_Position = vec4(driftedPosition, 1.0) * modelViewProjection;
    vertexColor = aColor;
    if (primitiveType == PRIMITIVE_POINT) {
        gl_PointSize = 2 / distanceToCamera;
    }
}