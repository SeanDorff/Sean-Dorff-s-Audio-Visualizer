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
uniform float[150] rotationHistory;

mat3 rotationMatrix(in vec3 axis, in float angle);
mat3 rotationMatrix(in vec3 axis, in float angle)
{
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0f - c;
    
    return mat3(oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.z * axis.x + axis.y * s,
                oc * axis.x * axis.y + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s,
                oc * axis.z * axis.x - axis.y * s,  oc * axis.y * axis.z + axis.x * s,  oc * axis.z * axis.z + c          );
}

float accumulatedRotationHistory(in float generation)
{
    float result = 0.0f;
    for (int i = 0; i < generation; i++)
        result += rotationHistory[i];
    return result;
}

void main(void)
{
    vec3 driftedPosition = vec3(aPosition.xy, aPosition.z - aPosition.w * drift);
    distanceToCamera = abs(distance(driftedPosition, cameraPosition));
    if (primitiveType == PRIMITIVE_POINT) {
        // driftedPosition = driftedPosition * rotationMatrix(vec3(0.0f, 0.0f, 1.0f), aPosition.w * 0.05f);
        driftedPosition = driftedPosition * rotationMatrix(vec3(0.0f, 0.0f, 1.0f), accumulatedRotationHistory(aPosition.w));
        gl_PointSize = 2 / distanceToCamera;
    }
    gl_Position = vec4(driftedPosition, 1.0) * modelViewProjection;
    vertexColor = aColor;
}