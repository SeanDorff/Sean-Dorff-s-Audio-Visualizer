#version 450 core

layout (location = 0) in vec4 aPosition;
layout (location = 1) in vec4 aColor;
out vec4 vertexColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float drift;

void main(void)
{
    gl_Position = vec4(aPosition.xy, drift * aPosition.z * aPosition.w, 1.0f) * model * view * projection;
    vertexColor = aColor;
}