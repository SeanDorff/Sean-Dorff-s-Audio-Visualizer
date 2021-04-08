#version 450 core

layout (location = 0) in vec4 starPosition;
layout (location = 1) in vec4 starColor;
out vec4 starVertexColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float drift;

void main(void)
{
    gl_Position = vec4(starPosition.xy, starPosition.z + drift * starPosition.w, 1.0f) * model * view * projection;
    starVertexColor = starColor;
}