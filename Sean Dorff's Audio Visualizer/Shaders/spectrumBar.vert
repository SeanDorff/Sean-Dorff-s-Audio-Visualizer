#version 450 core

layout (location = 0) in vec4 spectrumPosition;
layout (location = 1) in vec4 spectrumColor;
out vec4 spectrumVertexColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float drift;

void main(void)
{
    gl_Position = vec4(spectrumPosition.xy, spectrumPosition.z - spectrumPosition.w * drift, 1.0) * model * view * projection;
    spectrumVertexColor = spectrumColor;
}