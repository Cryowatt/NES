using NES.CPU;
using NES.CPU.Mappers;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NES
{
    class Program
    {
        private const long masterClock = 236_250_000 / 11;
        private const double cpuClock = masterClock / 12.0;
        private static int instructionCount = 1;
        private static int skip = 0;

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
            using (NativeWindow nativeWindow = NativeWindow.Create())
            {
                nativeWindow.ContextCreated += NativeWindow_ContextCreated;
                nativeWindow.ContextDestroying += NativeWindow_ContextDestroying;
                nativeWindow.Render += NativeWindow_Render;
                nativeWindow.Create(0, 0, 256, 256, NativeWindowStyle.Overlapped);
                nativeWindow.Show();
                nativeWindow.Run();
            }

            // RunBasic();
            //using (NativeWindow nativeWindow = NativeWindow.Create())
            //{
            //    nativeWindow.Create(0, 0, 256, 256, NativeWindowStyle.Overlapped);
            //    nativeWindow.Show();
            //    var context = DeviceContext.Create(nativeWindow.Display, nativeWindow.Handle);
            //    Gl.GenBuffers()
            //    Gl.ClearColor(1.0f, 0.0f, 0.0f, 1.0f);
            //    Gl.Clear(ClearBufferMask.ColorBufferBit);
            //    // Model-view matrix selector
            //    Gl.MatrixMode(MatrixMode.Modelview);
            //    // Load (reset) to identity
            //    Gl.LoadIdentity();
            //    // Multiply with rotation matrix (around Z axis)
            //    //Gl.Rotate(_Angle, 0.0f, 0.0f, 1.0f);

            //    // Draw triangle using immediate mode (8 draw call)

            //    // Start drawing triangles
            //    Gl.Begin(PrimitiveType.Triangles);

            //    // Feed triangle data: color and position
            //    // Note: vertex attributes (color, texture coordinates, ...) are specified before position information
            //    // Note: vertex data is passed using method calls (performance killer!)
            //    Gl.Color3(1.0f, 0.0f, 0.0f); Gl.Vertex2(0.0f, 0.0f);
            //    Gl.Color3(0.0f, 1.0f, 0.0f); Gl.Vertex2(0.5f, 1.0f);
            //    Gl.Color3(0.0f, 0.0f, 1.0f); Gl.Vertex2(1.0f, 0.0f);
            //    // Triangles ends
            //    Gl.End();
            //    nativeWindow.Run();
            //}

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

        private static void NativeWindow_ContextDestroying(object sender, NativeWindowEventArgs e)
        {
            Gl.DeleteVertexArrays(vao);
            Gl.DeleteBuffers(vbo);
            Gl.DeleteBuffers(ebo);
        }

        static uint vertexShader;
        static uint fragmentShader;
        static uint shaderProgram;
        static uint vbo;
        static uint vao;
        static uint ebo;

        private unsafe static void NativeWindow_ContextCreated(object sender, NativeWindowEventArgs e)
        {
            vertexShader = CreateVertexShader();
            fragmentShader = CreateFragmentShader(vertexShader);
            shaderProgram = CreateShaderProgram(vertexShader, fragmentShader);
            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);

            var vertices = new float[] {
                -1.0f, -2.0f, 0.0f,  // bottom left
                -1.0f,  1.0f, 0.0f,  // top left 
                 2.0f,  1.0f, 0.0f,  // top right
                 //0.5f, -0.5f, 0.0f,  // bottom right
            };

            var indices = new uint[] {  // note that we start from 0!
                0, 1, 2,  // first Triangle
                //1, 2, 3   // second Triangle
            };

            uint[] ids = new uint[1];
            Gl.GenVertexArrays(ids);
            vao = ids[0];
            Gl.GenBuffers(ids);
            vbo = ids[0];
            Gl.GenBuffers(ids);
            ebo = ids[0];

            // bind the Vertex Array Object first, then bind and set vertex buffer(s), and then configure vertex attributes(s).
            Gl.BindVertexArray(vao);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length), vertices, BufferUsage.StaticDraw);

            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            Gl.BufferData(BufferTarget.ElementArrayBuffer, (uint)(sizeof(uint) * indices.Length), indices, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 3 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            // note that this is allowed, the call to glVertexAttribPointer registered VBO as the vertex attribute's bound vertex buffer object so afterwards we can safely unbind
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // remember: do NOT unbind the EBO while a VAO is active as the bound element buffer object IS stored in the VAO; keep the EBO bound.
            //glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);

            // You can unbind the VAO afterwards so other VAO calls won't accidentally modify this VAO, but this rarely happens. Modifying other
            // VAOs requires a call to glBindVertexArray anyways so we generally don't unbind VAOs (nor VBOs) when it's not directly necessary.
            Gl.BindVertexArray(0);
        }

        static long frame = 0;
        static Random random = new Random();

        private unsafe static void NativeWindow_Render(object sender, NativeWindowEventArgs e)
        {
            Gl.ClearColor(0.5f, 0.0f, 0.5f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            Gl.UseProgram(shaderProgram);
            Gl.BindVertexArray(vao);
            Gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero);

            //Gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            //e.DeviceContext.SwapBuffers();
        }

        private static unsafe uint CreateShaderProgram(uint vertexShader, uint fragmentShader)
        {
            var shaderProgram = Gl.CreateProgram();

            Gl.AttachShader(shaderProgram, vertexShader);
            Gl.AttachShader(shaderProgram, fragmentShader);
            Gl.LinkProgram(shaderProgram);

            Gl.GetProgram(shaderProgram, ProgramProperty.LinkStatus, out int success);
            if (success == 0)
            {
                var message = new StringBuilder();
                Gl.GetProgramInfoLog(shaderProgram, 512, out int length, message);
                throw new Exception(message.ToString());
            }

            return shaderProgram;
        }

        private static unsafe uint CreateFragmentShader(uint vertexShader)
        {
            var fragmentShaderSource = @"#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
}".Split("\n");

            var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, fragmentShaderSource, fragmentShaderSource.Select(o => o.Length).ToArray());
            Gl.CompileShader(fragmentShader);
            Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);

            if (success == 0)
            {
                var message = new StringBuilder();
                Gl.GetShaderInfoLog(fragmentShader, 512, out int length, message);
                throw new Exception(message.ToString());
            }

            return fragmentShader;
        }

        private static unsafe uint CreateVertexShader()
        {
            var vertexShaderSource = @"#version 330 core
layout (location = 0) in vec3 aPos;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
}".Split("\n");

            var vertexShader = Gl.CreateShader(ShaderType.VertexShader);

            Gl.ShaderSource(vertexShader, vertexShaderSource, vertexShaderSource.Select(o => o.Length).ToArray());
            Gl.CompileShader(vertexShader);
            Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int success);

            if (success == 0)
            {
                var message = new StringBuilder();
                Gl.GetShaderInfoLog(vertexShader, 512, out int length, message);
                throw new Exception(message.ToString());
            }

            return vertexShader;
        }

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
        private static TimeSpan TotalStateTime = TimeSpan.Zero;

        private static void Statecpu(IMapper mapper, int runCycles)
        {
            instructionCount = 1;

            var bus = new NesBus(mapper);
            bus.Write(0x6001, 0xc0);
            var cpu = new Ricoh2A(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            cpu.InstructionTrace += OnInstructionTrace;
            var process = cpu.Process();
            var timer = Stopwatch.StartNew();
            foreach (var cycle in process.Take(runCycles))
            {
                if (instructionCount + 20 > skip)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpu.CycleCount, cycle);
                    Console.ResetColor();
                }
            }
            timer.Stop();
            TotalStateTime += timer.Elapsed;
            Console.WriteLine($"{runCycles} in {timer.Elapsed} actual. Relative speed: {runCycles / (timer.Elapsed.TotalSeconds * cpuClock):P}");
        }

        private static void BothCpu(RomImage rom, int runCycles)
        {
            instructionCount = 1;
            var funcBus = new NesBus(new Mapper0(rom));
            var stateBus = new NesBus(new Mapper0(rom));
            funcBus.Write(0x6001, 0xc0);
            stateBus.Write(0x6001, 0xc0);
            var cpus = new Ricoh2A(stateBus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            var cpuf = new Ricoh2AFunctional(funcBus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            cpus.InstructionTrace += OnInstructionTrace;
            cpuf.InstructionTrace += OnInstructionTracez;
            var process = cpus.Process();
            cpuf.Reset();
            var timer = Stopwatch.StartNew();
            foreach (var cycle in process.Take(runCycles))
            {
                var fcycle = cpuf.DoCycle();
                if (instructionCount + 20 > skip)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpus.CycleCount, cycle);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpuf.CycleCount, fcycle);
                    Console.ResetColor();
                }
            }
            timer.Stop();
            TotalStateTime += timer.Elapsed;
            Console.WriteLine($"{runCycles} in {timer.Elapsed} actual. Relative speed: {runCycles / (timer.Elapsed.TotalSeconds * cpuClock):P}");
        }

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
