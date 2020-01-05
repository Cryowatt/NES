#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 Texture;
layout(location = 0) out vec2 TexCoord;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    TexCoord = vec2(Texture.x, Texture.y);
}