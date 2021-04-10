#version 450

in vec4 vertexColor;
in float distanceToCamera;
out vec4 outputColor;

void main()
{
    outputColor = vertexColor * pow(0.97f, distanceToCamera * 8);
}