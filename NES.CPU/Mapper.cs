using NES.CPU.Mappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace NES.CPU
{
    public static class Mapper
    {
        public static IMapper FromImage(RomImage image)
        {
            return image.Mapper switch
            {
                0 => new Mapper0(image),
                1 => new Mapper1(image),
                _ => throw new NotImplementedException($"Unknown mapper {image.Mapper}")
            };
        }
    }
}
