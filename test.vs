#version 330 core
layout (location = 0) in vec3 aPos;
out vec3 screenCoord;
void main()
{
    gl_Position = vec4(aPos, 1.0);
    screenCoord = (aPos + 1.0) / 2.0;
}