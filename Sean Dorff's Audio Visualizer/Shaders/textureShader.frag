#version 460

in vec2 st;
uniform sampler2D tex;
uniform vec4 textColor;
out vec4 fragColor;

void main(void)
{
    fragColor = texture (tex, st) * textColor;
}