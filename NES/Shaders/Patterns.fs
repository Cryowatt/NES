#version 450
layout(location = 0) in vec2 TexCoord;
layout(location = 0) out vec4 OutColor;

layout(binding = 0) uniform sampler2D texture1;
layout(binding = 1) uniform sampler2D texture2;
//layout(binding = 1) uniform Tile {
// int topLow;
// int bottomLow;
// int topHigh;
// int bottomHigh;
//} tiles[256];

void main()
{
    OutColor = texture(texture1, TexCoord)+texture(texture2, TexCoord);
    //OutColor = vec4(tiles[0].topLow);
}