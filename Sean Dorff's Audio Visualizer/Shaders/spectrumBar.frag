﻿#version 450

in vec4 spectrumVertexColor;
out vec4 outputColor;

void main()
{
//    outputColor = vec4(1.0f,1.0f,1.0f,1.0f);
    outputColor = spectrumVertexColor;
}