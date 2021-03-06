﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NES
{
    public static class Shaders
    {
        public static readonly string VertexCode = File.ReadAllText("./Shaders/Passthrough.vs");
        
//        @"
//#version 450
//layout(location = 0) in vec2 Position;
//layout(location = 1) in vec2 Texture;
//layout(location = 0) out vec2 TexCoord;

//void main()
//{
//    gl_Position = vec4(Position, 0, 1);
//    TexCoord = vec2(Texture.x, Texture.y);
//}";

        public static readonly string FragmentCode = File.ReadAllText("./Shaders/Patterns.fs");
//        @"
//#version 450
//layout(location = 0) in vec2 TexCoord;
//layout(location = 0) out vec4 OutColor;

//struct Tile
//{
//  byte low[8];
//  byte high[8];
//};

//layout(binding = 0) uniform sampler2D texture1;
//layout(binding = 1) uniform Tile patterns;

//void main()
//{
//    OutColor = texture(texture1, TexCoord);
//}";
        //            var fragmentShaderSource = @"#version 330 core
        //out vec4 FragColor;

        //in vec3 ourColor;
        //in vec2 TexCoord;

        //// texture samplers
        //uniform sampler2D texture;

        //void main()
        //{
        //	// linearly interpolate between both textures (80% container, 20% awesomeface)
        //	//FragColor = mix(texture(texture1, TexCoord), texture(texture2, TexCoord), 0.2);
        //    FragColor = texture(texture, TexCoord);
        //    //FragColor = texelFetch(texture, ivec2(gl_FragCoord.xy), 0);
        //}".Split("\n");
    }
}