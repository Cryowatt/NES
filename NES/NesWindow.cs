using NES.CPU;
using NES.CPU.Mappers;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace NES
{
    public struct VertexTexture
    {
        public Vector2 Position;
        public Vector2 Texture;

        public VertexTexture(Vector2 position, Vector2 texture)
        {
            this.Position = position;
            this.Texture = texture;
        }
    }

    public class NesWindow : IDisposable
    {
        private const int PatternsTableSize = 16 * 16 * 8 * 2;
        private Sdl2Window window;
        private GraphicsDevice device;
        private CommandList commandList;
        private DeviceBuffer vertexBuffer;
        private DeviceBuffer indexBuffer;
        private Shader[] shaders;
        private Pipeline pipeline;
        private int frames = 0;
        private Stopwatch timer = Stopwatch.StartNew();
        private DeviceBuffer patternsBuffer;
        private Texture nametableTexture;
        private Texture patternTexture;
        private TextureView nametableTextureView;
        private TextureView patternTextureView;
        private readonly Memory<Vector4> patternBuffer = new Memory<Vector4>(new Vector4[16 * 8 * 16 * 8]);
        private readonly Memory<Vector4> nametableBuffer = new Memory<Vector4>(new Vector4[256 * 240 * 4]);
        private KeyboardInputDevice input1 = new KeyboardInputDevice();

        public unsafe struct TileData
        {
            public fixed byte LowPlane[8];
            public fixed byte HighPlane[8];
        }

        private unsafe static void RenderNameTable(byte* nametableData, Vector4* nametableBuffer, TileData* patternData)
        {
            for (int tileY = 0; tileY < 30; tileY++)
            {
                for (int tileX = 0; tileX < 32; tileX++)
                {
                    var nametableIndex = *nametableData++;
                    //nametableIndex = (byte)rand.Next();
                    var tile = *(patternData + nametableIndex);

                    for (int y = 0; y < 8; y++)
                    {
                        var lowPlane = tile.LowPlane[y];
                        var highPlane = tile.HighPlane[y];

                        for (int x = 0; x < 8; x++)
                        {
                            int offset = 7 - x;
                            var colorIndex = (lowPlane >> offset) & 0x1 | ((highPlane >> offset) & 0x1) << 1;
                            Vector4 color;
                            color.X = color.Y = color.Z = colorIndex / 3.0f;
                            color.W = 1.0f;
                            *(nametableBuffer + x + (tileX * 8) + (y * 32 * 8) + (tileY * 8 * 32 * 8)) = color;
                        }
                    }
                }
            }
        }

        private unsafe static void RenderPatternTable(TileData* patternData, Vector4* patternBuffer)
        {
            for (int tileY = 0; tileY < 16; tileY++)
            {
                for (int tileX = 0; tileX < 16; tileX++)
                {
                    var tile = *(patternData++);

                    for (int y = 0; y < 8; y++)
                    {
                        var lowPlane = tile.LowPlane[y];
                        var highPlane = tile.HighPlane[y];
                        var pTexture = patternBuffer + (tileX * 8) + (y * 8 * 16) + (tileY * 8 * 16 * 8);

                        for (int x = 0; x < 8; x++)
                        {
                            int offset = 7 - x;
                            var colorIndex = (lowPlane >> offset) & 0x1 | ((highPlane >> offset) & 0x1) << 1;
                            Vector4 color;
                            color.X = color.Y = color.Z = colorIndex / 3.0f;
                            color.W = 1.0f;
                            *(pTexture++) = color;
                        }
                    }
                }
            }
        }

        private static Vector3 GetPalette(int index)
        {
            return new Vector3(index / 3.0f);
        }

        public NesWindow()
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                WindowWidth = 768,
                WindowHeight = 480,
                WindowTitle = "NES",
                WindowInitialState = WindowState.Normal
            };
            VeldridStartup.CreateWindowAndGraphicsDevice(windowCI, out window, out device);
            CreateResources();
        }

        public void StartNes()
        {
            RomImage romFile;
            //var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\01-basics.nes");
            var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\official_only.nes");
            //var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\nestest.nes");
            //var stream = File.OpenRead(@"C:\Users\ericc\Source\Repos\Cryowatt\NES\NES\Donkey Kong (JU).nes");
            using (var reader = new BinaryReader(stream))
            {
                romFile = RomImage.From(reader);
            }

            var mapper = Mapper.FromImage(romFile);
            platform = new NesBus(mapper, input1: input1);
            platform.OnInstruction += OnInstruction;

            this.Nametable = platform.PatternTable.Pin();
            platform.Reset();
            Task.Factory.StartNew(platform.Run);
        }

        private void OnInstruction(InstructionTrace obj)
        {
            Console.WriteLine(obj);
        }

        public void Run()
        {
            StartNes();

            while (window.Exists)
            {
                var input = window.PumpEvents();
                input1.SetInputSnapshot(input);
                Draw();
                frames++;

                if (timer.ElapsedMilliseconds > 1000)
                {
                    //Console.WriteLine($"{frames}fps");
                    frames = 0;
                    timer.Restart();
                }
            }
        }

        private unsafe void CreateResources()
        {
            VertexTexture[] quadVertices =
            {
                // Full screen
                new VertexTexture(new Vector2(-1f, 1f), new Vector2(0f, 1f)),// Top Left
                new VertexTexture(new Vector2(1f, 1f), new Vector2(1f, 1f)),// Top Right
                new VertexTexture(new Vector2(-1f, -1f), new Vector2(0f, 0f)),// Bottom Left
                new VertexTexture(new Vector2(1f, -1f), new Vector2(1f, 0f)),// Bottom Right

                // Debug nametable
                new VertexTexture(new Vector2(-1f, 1f), new Vector2(0f, 0f)),// Top Left
                new VertexTexture(new Vector2(1/3f, 1f), new Vector2(1f, 0f)),// Top Right
                new VertexTexture(new Vector2(-1f, -1f), new Vector2(0f, 1f)),// Bottom Left
                new VertexTexture(new Vector2(1/3f, -1f), new Vector2(1f, 1f)),// Bottom Right

                // Debug pattern table
                new VertexTexture(new Vector2(1/3f, 1f), new Vector2(0f, 0f)),// Top Left
                new VertexTexture(new Vector2(1f, 1f), new Vector2(1f, 0f)),// Top Right
                new VertexTexture(new Vector2(1/3f, 7/15f), new Vector2(0f, 1f)),// Bottom Left
                new VertexTexture(new Vector2(1f, 7/15f), new Vector2(1f, 1f)),// Bottom Right
            };

            BufferDescription vbDescription = new BufferDescription(
                (uint)(quadVertices.Length * sizeof(VertexTexture)),
                BufferUsage.VertexBuffer);
            vertexBuffer = device.ResourceFactory.CreateBuffer(vbDescription);
            device.UpdateBuffer(vertexBuffer, 0, quadVertices);

            ushort[] quadIndices = { 0, 1, 2, 3 };
            BufferDescription ibDescription = new BufferDescription(
                (uint)(quadIndices.Length * sizeof(ushort)),
                BufferUsage.IndexBuffer);
            indexBuffer = device.ResourceFactory.CreateBuffer(ibDescription);
            device.UpdateBuffer(indexBuffer, 0, quadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Texture", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            patternsBuffer = device.ResourceFactory.CreateBuffer(
                new BufferDescription(PatternsTableSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            nametableTexture = device.ResourceFactory.CreateTexture(
                new TextureDescription(512, 480, 1, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled, TextureType.Texture2D));
            nametableTextureView = device.ResourceFactory.CreateTextureView(nametableTexture);

            patternTexture = device.ResourceFactory.CreateTexture(
                new TextureDescription(256, 128, 1, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled, TextureType.Texture2D));
            patternTextureView = device.ResourceFactory.CreateTextureView(patternTexture);

            ResourceLayout patternTextureLayout = device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("PatternTable", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            patternTextureSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                 patternTextureLayout,
                 patternTextureView,
                 device.PointSampler));

            ResourceLayout nametableTextureLayout = device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("NameTable", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            nametableTextureSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                 nametableTextureLayout,
                 nametableTextureView,
                 device.PointSampler));

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(Shaders.VertexCode),
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(Shaders.FragmentCode),
                "main");

            shaders = device.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            // Create pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new ResourceLayout[]
                {
                    nametableTextureLayout,
                    patternTextureLayout,
                },
                ShaderSet = new ShaderSetDescription(
                    vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                    shaders: shaders),
                Outputs = device.SwapchainFramebuffer.OutputDescription,
            };

            pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

            commandList = device.ResourceFactory.CreateCommandList();
        }

        private static Random rand = new Random();
        private NesBus platform;
        private ResourceSet patternTextureSet;
        private ResourceSet nametableTextureSet;

        public MemoryHandle Nametable { get; internal set; }

        private unsafe void Draw()
        {
            //using var fpPattern = patternData.Pin();
            //RenderPatternTable(fpBuffer.)
            //16x16 (8x8 tiles)
            using (var fpBuffer = patternBuffer.Pin())
            {
                using (var pattern = this.platform.PatternTable.Slice(0, 0x1000).Pin())
                {
                    RenderPatternTable((TileData*)pattern.Pointer, (Vector4*)fpBuffer.Pointer);
                    device.UpdateTexture(
                        patternTexture,
                        (IntPtr)fpBuffer.Pointer,
                        16 * 8 * 16 * 8 * 4 * sizeof(float), // 16x16 tiles, 8 bytes per plane, 2 planes per tile
                        0, 0, 0, 128, 128, 1, 0, 0);
                }

                using (var pattern = this.platform.PatternTable.Slice(0x1000, 0x1000).Pin())
                {
                    RenderPatternTable((TileData*)pattern.Pointer, (Vector4*)fpBuffer.Pointer);
                    device.UpdateTexture(
                        patternTexture,
                        (IntPtr)fpBuffer.Pointer,
                        16 * 8 * 16 * 8 * 4 * sizeof(float), // 16x16 tiles, 8 bytes per plane, 2 planes per tile
                        128, 0, 0, 128, 128, 1, 0, 0);
                }
            }

            using (var nametable = this.platform.Nametable.Pin())
            {
                using (var fpNametable = nametableBuffer.Pin())
                {
                    var patternBankOffset = (this.platform.PPU.Registers.PPUCTRL & PPUControl.BGBank2) > 0 ? 0x1000 : 0x0;
                    using (var pattern = this.platform.PatternTable.Slice(patternBankOffset).Pin())
                    {
                        RenderNameTable((byte*)nametable.Pointer + 0x000, (Vector4*)fpNametable.Pointer, (TileData*)pattern.Pointer);
                        device.UpdateTexture(
                            nametableTexture,
                            (IntPtr)fpNametable.Pointer,
                            (uint)(256 * 240 * sizeof(Vector4)),
                            0, 0, 0, 256, 240, 1, 0, 0);

                        RenderNameTable((byte*)nametable.Pointer + 0x400, (Vector4*)fpNametable.Pointer, (TileData*)pattern.Pointer);
                        device.UpdateTexture(
                            nametableTexture,
                            (IntPtr)fpNametable.Pointer,
                            (uint)(256 * 240 * sizeof(Vector4)),
                            256, 0, 0, 256, 240, 1, 0, 0);

                        RenderNameTable((byte*)nametable.Pointer + 0x800, (Vector4*)fpNametable.Pointer, (TileData*)pattern.Pointer);
                        device.UpdateTexture(
                            nametableTexture,
                            (IntPtr)fpNametable.Pointer,
                            (uint)(256 * 240 * sizeof(Vector4)),
                            0, 240, 0, 256, 240, 1, 0, 0);

                        RenderNameTable((byte*)nametable.Pointer + 0xC00, (Vector4*)fpNametable.Pointer, (TileData*)pattern.Pointer);
                        device.UpdateTexture(
                            nametableTexture,
                            (IntPtr)fpNametable.Pointer,
                            (uint)(256 * 240 * sizeof(Vector4)),
                            256, 240, 0, 256, 240, 1, 0, 0);
                    }
                }
            }

            commandList.Begin();
            commandList.SetFramebuffer(device.SwapchainFramebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Grey);

            // Set all relevant state to draw our quad.
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetPipeline(pipeline);

            // Issue a Draw command for a single instance with 4 indices.
            commandList.SetGraphicsResourceSet(0, nametableTextureSet);
            commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 4,
                instanceStart: 0);

            // Patterns table
            commandList.SetGraphicsResourceSet(0, patternTextureSet);
            commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 8,
                instanceStart: 0);
            // End() must be called before commands can be submitted for execution.

            commandList.End();
            device.SubmitCommands(commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            device.SwapBuffers();
            device.WaitForIdle();
        }

        public void Dispose()
        {
            this.commandList.Dispose();
        }
    }
}
