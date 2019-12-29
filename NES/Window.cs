using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
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

    public class Window : IDisposable
    {
        private Sdl2Window window;
        private GraphicsDevice device;
        private CommandList commandList;
        private DeviceBuffer vertexBuffer;
        private DeviceBuffer indexBuffer;
        private Shader[] shaders;
        private Pipeline pipeline;
        private int frames = 0;
        private Stopwatch timer = Stopwatch.StartNew();
        private Texture nametableTexture;
        private TextureView nametableTextureView;
        private ResourceSet debugTextureSet;

        public Window()
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                WindowWidth = 768*2,
                WindowHeight = 480*2,
                WindowTitle = "NES"
            };
            VeldridStartup.CreateWindowAndGraphicsDevice(windowCI, out window, out device);
            CreateResources();
        }

        public void Run()
        {
            while (window.Exists)
            {
                var input = window.PumpEvents();
                Draw();
                frames++;

                if (timer.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"{frames}fps");
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
                new VertexTexture(new Vector2(-1f, 1f), new Vector2(0f, 1f)),// Top Left
                new VertexTexture(new Vector2(1/3f, 1f), new Vector2(1f, 1f)),// Top Right
                new VertexTexture(new Vector2(-1f, -1f), new Vector2(0f, 0f)),// Bottom Left
                new VertexTexture(new Vector2(1/3f, -1f), new Vector2(1f, 0f)),// Bottom Right

                // Debug pattern table left
                new VertexTexture(new Vector2(1/3f, 1f), new Vector2(0f, 1f)),// Top Left
                new VertexTexture(new Vector2(2/3f, 1f), new Vector2(1f, 1f)),// Top Right
                new VertexTexture(new Vector2(1/3f, 0f), new Vector2(0f, 0f)),// Bottom Left
                new VertexTexture(new Vector2(2/3f, 0f), new Vector2(1f, 0f)),// Bottom Right

                // Debug pattern table right
                new VertexTexture(new Vector2(2/3f, 1f), new Vector2(0f, 1f)),// Top Left
                new VertexTexture(new Vector2(1f, 1f), new Vector2(1f, 1f)),// Top Right
                new VertexTexture(new Vector2(2/3f, 0f), new Vector2(0f, 0f)),// Bottom Left
                new VertexTexture(new Vector2(1f, 0f), new Vector2(1f, 0f)),// Bottom Right
            };

            BufferDescription vbDescription = new BufferDescription(
                (uint)(quadVertices.Length * sizeof(VertexTexture)),
                BufferUsage.VertexBuffer);
            vertexBuffer = device.ResourceFactory.CreateBuffer(vbDescription);
            device.UpdateBuffer(vertexBuffer, 0, quadVertices);

            ushort[] quadIndices = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15 };
            BufferDescription ibDescription = new BufferDescription(
                (uint)(quadIndices.Length * sizeof(ushort)),
                BufferUsage.IndexBuffer);
            indexBuffer = device.ResourceFactory.CreateBuffer(ibDescription);
            device.UpdateBuffer(indexBuffer, 0, quadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Texture", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            nametableTexture = device.ResourceFactory.CreateTexture(new TextureDescription(512, 480, 1, 1, 1, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));
            nametableTextureView = device.ResourceFactory.CreateTextureView(nametableTexture);

            ResourceLayout worldTextureLayout = device.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("NametableTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            debugTextureSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                 worldTextureLayout,
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
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = new ResourceLayout[] { worldTextureLayout };
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: shaders);
            pipelineDescription.Outputs = device.SwapchainFramebuffer.OutputDescription;

            pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

            commandList = device.ResourceFactory.CreateCommandList();
        }

        private static Random rand = new Random();
        Vector4[] texture = new Vector4[512 * 480];

        public MemoryHandle Nametable { get; internal set; }

        private unsafe void Draw()
        {
            for (int i = 0; i < 512 * 480; i++)
            {
                //    using (var bufferPin = platform.FrameBuffer.Pin())
                //    {
                //        Gl.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, 256, 240, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)bufferPin.Pointer);
                //    }
                texture[i].X = (float)rand.NextDouble();
                //texture[i].Y = (float)rand.NextDouble();
                //texture[i].Z = (float)rand.NextDouble();
            }

            commandList.Begin();
            device.UpdateTexture(
                nametableTexture,
                (IntPtr)Nametable.Pointer, 256 * 240 * sizeof(int),
                0, 0, 0, 256, 240, 1, 0, 0);
            commandList.SetFramebuffer(device.SwapchainFramebuffer);

            // Set all relevant state to draw our quad.
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetPipeline(pipeline);
            commandList.SetGraphicsResourceSet(0, debugTextureSet);
            // Issue a Draw command for a single instance with 4 indices.
            commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 4,
                vertexOffset: 0,
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
