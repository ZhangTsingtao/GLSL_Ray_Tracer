#version 330 core
in vec3 screenCoord;
out vec4 FragColor;

void main()
{
    FragColor = vec4(screenCoord.xy, 0.2, 1.0f);
}