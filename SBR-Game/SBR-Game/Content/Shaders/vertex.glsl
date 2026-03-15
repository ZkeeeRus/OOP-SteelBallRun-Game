#version 330 core

layout (location = 0) in vec3 aPosition;
uniform vec3 uOffset;

void main()
{
    gl_Position = vec4(aPosition + uOffset, 1.0);
}