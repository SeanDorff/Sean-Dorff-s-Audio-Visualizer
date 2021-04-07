#version 450

in vec4 vertexColor;
out vec4 outputColor;

uniform float loudness;

void main()
{
//    outputColor = vec4(1.0f,1.0f,1.0f,1.0f);
    outputColor = vec4(loudness, vertexColor.yzw);
}