#version 460
#define PRIMITIVE_POINT 0
#define PRIMITIVE_TRIANGLE 1

layout (location = 0) in vec2 vp;
layout (location = 1) in vec2 vt;
out vec2 st;

void main(void)
{
    st = vt;
    gl_Position = vec4(vp, 0.0, 1.0);
}