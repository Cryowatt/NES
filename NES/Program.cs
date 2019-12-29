using NES.CPU;
using NES.CPU.Mappers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NES
{

    class Program
    {
        private const long masterClock = 236_250_000 / 11;
        private const double cpuClock = masterClock / 12.0;
        private static int instructionCount = 1;
        private static int skip = 0;
        private static Platform platform;

        public class TestMapper : IMapper
        {
            public AddressRange AddressRange { get; } = new AddressRange(0x6000, 0xffff);

            public byte Read(Address address)
            {
                if (address == 0x6ffc)
                {
                    return 0x4c;
                }
                else if (address == 0x6ffd)
                {
                    return 0x00;
                }
                else if (address == 0x6ffe)
                {
                    return 0x60;
                }
                else
                {
                    return 0x80;
                }
            }

            public void Write(Address address, byte value)
            {
                throw new NotImplementedException();
            }
        }

        static void Main(string[] args)
        {
            RomImage romFile;
            //var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\01-basics.nes");
            var stream = File.OpenRead(@"C:\Users\ericc\Source\Repos\Cryowatt\NES\NES\Donkey Kong (JU).nes");
            using (var reader = new BinaryReader(stream))
            {
                romFile = RomImage.From(reader);
            }

            var mapper = new Mapper0(romFile);
            platform = new Platform(mapper);
            platform.Reset();
            Task.Factory.StartNew(platform.Run);

            var window = new Window();
            window.Nametable = platform.FrameBuffer.Pin();
            //    using (var bufferPin = platform.FrameBuffer.Pin())
            //    {
            //        Gl.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, 256, 240, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)bufferPin.Pointer);
            //    }
            window.Run();

            //RunBasic();

            //if (args.Length > 0)
            //{
            //    skip = int.Parse(args[0]);
            //}
            //var runCycles = 26559;
            //RomImage romFile = LoadRom();
            //var mapper = new Mapper0(romFile);

            //// ========= NOP shit ============
            ////var mapper = new TestMapper();
            ////var runCycles = (int)cpuClock;

            ////BothCpu(romFile, runCycles);
            //Funccpu(mapper, runCycles);
            ////Statecpu(mapper, runCycles);
            //TotalFuncTime = TimeSpan.Zero;
            //TotalStateTime = TimeSpan.Zero;

            //for (int i = 0; i < 20; i++)
            //{
            //    //Console.WriteLine("State Machine CPU");
            //    //Statecpu(mapper, runCycles);
            //    Console.WriteLine("Functional CPU");
            //    Funccpu(mapper, runCycles);
            //}

            //Console.WriteLine("Total Func: " + TotalFuncTime);
            //Console.WriteLine("Total Stat: " + TotalStateTime);
        }

        //private unsafe static void UpdateFrameBuffer()
        //{
        //    using (var pin = platform.FrameBuffer.Pin())
        //    {
        //        fixed (byte* pFrameBuffer = frameBuffer)
        //        {
        //            int* pFrameBuffer = (int*)pin.Pointer;
        //            //int offset = (int)frame; //random.Next();
        //            int* ptr = (int*)pFrameBuffer;

        //            for (int i = 0; i < 256 * 240; i++)
        //            {
        //                *(ptr++) = (((i & 0x1) * 0xff) << 8) | 0xff;
        //            }
        //        }
        //    }
        //}

        static long frame = 0;
        static Random random = new Random();
        static Stopwatch frameRateTimer = Stopwatch.StartNew();
        static Stopwatch frameTimer = Stopwatch.StartNew();
        static byte[] frameBuffer = new byte[256 * 240 * 4];

        //private unsafe static void NativeWindow_Render(object sender, NativeWindowEventArgs e)
        //{
        //    Gl.ClearColor(0.5f, 0.0f, 0.5f, 1.0f);
        //    Gl.Clear(ClearBufferMask.ColorBufferBit);

        //    //UpdateFrameBuffer();
        //    //var data = Enumerable.Range(random.Next(), 256 * 256).SelectMany(o => new byte[] { (byte)o, (byte)(o >> 8), (byte)(o >> 16), byte.MaxValue }).ToArray();
        //    using (var bufferPin = platform.FrameBuffer.Pin())
        //    {
        //        Gl.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, 256, 240, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)bufferPin.Pointer);
        //    }
        //    //Gl.GenerateMipmap(TextureTarget.Texture2d);

        //    // bind textures on corresponding texture units
        //    Gl.ActiveTexture(TextureUnit.Texture0);
        //    Gl.BindTexture(TextureTarget.Texture2d, texture);

        //    Gl.UseProgram(shaderProgram);
        //    Gl.BindVertexArray(vao);
        //    Gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero);

        //    //Gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        //    //e.DeviceContext.SwapBuffers();
        //    frame++;
        //    if (frameRateTimer.ElapsedMilliseconds > 1000)
        //    {
        //        Console.WriteLine($"{frame}fps");
        //        frame = 0;
        //        frameRateTimer.Restart();
        //    }

        //    var delay = 15 - (int)frameTimer.ElapsedMilliseconds;
        //        //Console.WriteLine(delay);
        //    if (delay > 0)
        //    {
        //        Thread.Sleep(delay);
        //    }
        //    frameTimer.Restart();
        //}

        //private static unsafe uint CreateShaderProgram(uint vertexShader, uint fragmentShader)
        //{
        //    var shaderProgram = Gl.CreateProgram();

        //    Gl.AttachShader(shaderProgram, vertexShader);
        //    Gl.AttachShader(shaderProgram, fragmentShader);
        //    Gl.LinkProgram(shaderProgram);

        //    Gl.GetProgram(shaderProgram, ProgramProperty.LinkStatus, out int success);
        //    if (success == 0)
        //    {
        //        var message = new StringBuilder();
        //        Gl.GetProgramInfoLog(shaderProgram, 512, out int length, message);
        //        throw new Exception(message.ToString());
        //    }

        //    return shaderProgram;
        //}

        //        private static unsafe uint CreateFragmentShader(uint vertexShader)
        //        {
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

        //            var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        //            Gl.ShaderSource(fragmentShader, fragmentShaderSource, fragmentShaderSource.Select(o => o.Length).ToArray());
        //            Gl.CompileShader(fragmentShader);
        //            Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);

        //            if (success == 0)
        //            {
        //                var message = new StringBuilder();
        //                Gl.GetShaderInfoLog(fragmentShader, 512, out int length, message);
        //                throw new Exception(message.ToString());
        //            }

        //            return fragmentShader;
        //        }

        //        private static unsafe uint CreateVertexShader()
        //        {
        //            var vertexShaderSource = @"#version 330 core
        //layout (location = 0) in vec3 aPos;
        //layout (location = 1) in vec3 aColor;
        //layout (location = 2) in vec2 aTexCoord;

        //out vec3 ourColor;
        //out vec2 TexCoord;

        //void main()
        //{
        //    gl_Position = vec4(aPos, 1.0);
        //	//ourColor = aColor;
        //	TexCoord = vec2(aTexCoord.x, aTexCoord.y);
        //}".Split("\r\n");

        //            var vertexShader = Gl.CreateShader(ShaderType.VertexShader);

        //            Gl.ShaderSource(vertexShader, vertexShaderSource, vertexShaderSource.Select(o => o.Length).ToArray());
        //            Gl.CompileShader(vertexShader);
        //            Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);

        //            if (success == 0)
        //            {
        //                var message = new StringBuilder();
        //                Gl.GetShaderInfoLog(vertexShader, 512, out int length, message);
        //                throw new Exception(message.ToString());
        //            }

        //            return vertexShader;
        //        }

        private static void RunBasic()
        {
            //Console.BufferWidth
            RomImage romFile;
            var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\01-basics.nes");
            using (var reader = new BinaryReader(stream))
            {
                romFile = RomImage.From(reader);
            }

            var mapper = new Mapper0(romFile);
            var platform = new Platform(mapper);
            platform.Reset();
            platform.Run();
            //var bus = new NesBus(mapper);
            //var cpu = new Ricoh2AFunctional(bus);
            //cpu.Reset();

            //while (true)
            //{
            //    var cycle = platform.DoCycle();
            //}
        }

        private static RomImage LoadRom()
        {
            RomImage romFile;
            Console.WriteLine(Environment.CurrentDirectory);
            var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\nestest.nes");
            using (var reader = new BinaryReader(stream))
            {
                romFile = RomImage.From(reader);
            }

            return romFile;
        }

        private static TimeSpan TotalFuncTime = TimeSpan.Zero;

        private static void Funccpu(IMapper mapper, int runCycles)
        {
            instructionCount = 1;

            var bus = new NesBus(mapper);
            bus.Write(0x6001, 0xc0);
            var cpu = new Ricoh2AFunctional(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            cpu.InstructionTrace += OnInstructionTrace;
            //var process = cpu.Process();
            var timer = Stopwatch.StartNew();
            cpu.Reset();
            while (cpu.CycleCount < runCycles)
            {
                var cycle = cpu.DoCycle();
                if (instructionCount + 20 > skip)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpu.CycleCount, cycle);
                    Console.ResetColor();
                }
            }
            timer.Stop();
            TotalFuncTime += timer.Elapsed;
            Console.WriteLine($"{runCycles} in {timer.Elapsed} actual. Relative speed: {runCycles / (timer.Elapsed.TotalSeconds * cpuClock):P}");
        }

        private static void OnInstructionTrace(InstructionTrace trace)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (instructionCount + 20 > skip)
            {
                Console.WriteLine("[{0}] {1}", instructionCount, trace);
            }
            instructionCount++;
            if (instructionCount > skip)
            {
                Console.ReadLine();
            }
            Console.ResetColor();
        }

        private static void OnInstructionTracez(InstructionTrace trace)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (instructionCount + 20 > skip)
            {
                Console.WriteLine("[{0}] {1}", instructionCount, trace);
            }
            Console.ResetColor();
        }
    }
}
