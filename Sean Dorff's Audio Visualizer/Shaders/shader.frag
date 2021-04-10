#version 450

in vec4 vertexColor;
in float distanceToCamera;
out vec4 outputColor;
uniform float alphaDimm;

void main()
{
    outputColor = vertexColor * pow(alphaDimm, distanceToCamera * 8);
}