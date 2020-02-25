using System;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using TrippyGL;

namespace TrippyTesting.Tests
{
    class IndexBufferTest : GameWindow
    {
        System.Diagnostics.Stopwatch stopwatch;
        private float time;
        static Random r = new Random();

        private VertexColor[] vertexData;

        BufferObject bufferObject;
        VertexDataBufferSubset<VertexColor> vertexSubset;
        IndexBufferSubset indexSubset;
        VertexArray vertexArray;

        PrimitiveBatcher<VertexColor> extraLinesBatcher;
        VertexBuffer<VertexColor> extraLinesBuffer;

        ShaderProgram shaderProgram;

        GraphicsDevice graphicsDevice;

        public IndexBufferTest() : base(1280, 720, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 0, 0, 0, ColorFormat.Empty, 2), "haha yes", GameWindowFlags.Default, DisplayDevice.Default, 4, 0, GraphicsContextFlags.Default)
        {
            VSync = VSyncMode.On;

            graphicsDevice = new GraphicsDevice(Context);
            graphicsDevice.DebugMessagingEnabled = true;
            graphicsDevice.DebugMessage += Program.OnDebugMessage;

            Console.WriteLine(string.Concat("GL Version: ", graphicsDevice.GLMajorVersion, ".", graphicsDevice.GLMinorVersion));
            Console.WriteLine("GL Version String: " + graphicsDevice.GLVersion);
            Console.WriteLine("GL Vendor: " + graphicsDevice.GLVendor);
            Console.WriteLine("GL Renderer: " + graphicsDevice.GLRenderer);
            Console.WriteLine("GL ShadingLanguageVersion: " + graphicsDevice.GLShadingLanguageVersion);
            Console.WriteLine("GL TextureUnits: " + graphicsDevice.MaxTextureImageUnits);
            Console.WriteLine("GL MaxTextureSize: " + graphicsDevice.MaxTextureSize);
            Console.WriteLine("GL MaxSamples:" + graphicsDevice.MaxSamples);
        }

        protected override void OnLoad(EventArgs e)
        {
            TargetRenderFrequency = 3;
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            time = 0;

            shaderProgram = new ShaderProgram(graphicsDevice);
            shaderProgram.AddVertexShader(File.ReadAllText("indextest/vs.glsl"));
            shaderProgram.AddFragmentShader(File.ReadAllText("indextest/fs.glsl"));
            shaderProgram.SpecifyVertexAttribs<VertexColor>(new string[] { "vPosition", "vColor" });
            shaderProgram.LinkProgram();
            Matrix4 mat = Matrix4.CreateScale(0.9f);
            shaderProgram.Uniforms["mat"].SetValueMat4(ref mat);

            const int w = 5, h = 5;
            vertexData = new VertexColor[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    vertexData[x + y * w] = new VertexColor(new Vector3(x / (float)w * 2f - 1 + randomf(-0.2f, 0.2f), y / (float)h * 2f - 1 + randomf(-0.1f, 0.1f), 0), randomCol());

            bufferObject = new BufferObject(graphicsDevice, VertexColor.SizeInBytes * vertexData.Length + 128, BufferUsageHint.DynamicDraw);
            vertexSubset = new VertexDataBufferSubset<VertexColor>(bufferObject, 0, vertexData.Length, vertexData);
            indexSubset = new IndexBufferSubset(bufferObject, vertexSubset.StorageNextInBytes, 128, DrawElementsType.UnsignedByte);

            vertexArray = VertexArray.CreateSingleBuffer<VertexColor>(graphicsDevice, vertexSubset, indexSubset);

            extraLinesBatcher = new PrimitiveBatcher<VertexColor>(0, 32);
            extraLinesBuffer = new VertexBuffer<VertexColor>(graphicsDevice, extraLinesBatcher.LineVertexCapacity, BufferUsageHint.StreamDraw);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            time = (float)stopwatch.Elapsed.TotalSeconds;
            ErrorCode c;
            while ((c = GL.GetError()) != ErrorCode.NoError)
            {
                Console.WriteLine("Error found: " + c);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            graphicsDevice.ClearColor = new Color4(0f, 0f, 0f, 1f);
            graphicsDevice.BlendingEnabled = false;
            graphicsDevice.DepthTestingEnabled = false;

            graphicsDevice.Clear(ClearBufferMask.ColorBufferBit);

            graphicsDevice.VertexArray = vertexArray;
            graphicsDevice.ShaderProgram = shaderProgram;
            Span<byte> indices = stackalloc byte[4]
            {
                (byte)r.Next(vertexSubset.StorageLength),
                14,
                (byte)r.Next(vertexSubset.StorageLength),
                15
            };

            indexSubset.SetData(indices.ToArray());
            graphicsDevice.DrawElements(PrimitiveType.TriangleStrip, 1, 3);
            graphicsDevice.DrawArrays(PrimitiveType.Lines, 0, vertexSubset.StorageLength);

            for (int i = 1; i < indices.Length; i++)
            {
                Vector3 p = vertexData[indices[i]].Position;
                extraLinesBatcher.AddLine(new VertexColor(p, Color4b.Red), new VertexColor(new Vector3(p.X, p.Y + 0.5f, p.Z), Color4b.Red));
            }

            if (extraLinesBatcher.LineVertexCount > extraLinesBuffer.StorageLength)
                extraLinesBuffer.RecreateStorage(extraLinesBatcher.LineVertexCapacity);
            extraLinesBatcher.WriteLinesTo(extraLinesBuffer.DataSubset);
            graphicsDevice.VertexArray = extraLinesBuffer.VertexArray;
            graphicsDevice.DrawArrays(PrimitiveType.Lines, 0, extraLinesBatcher.LineVertexCount);
            extraLinesBatcher.ClearLines();

            SwapBuffers();
        }

        protected override void OnResize(EventArgs e)
        {
            graphicsDevice.Viewport = new Rectangle(0, 0, Width, Height);
        }

        protected override void OnUnload(EventArgs e)
        {
            shaderProgram.Dispose();
            bufferObject.Dispose();
            vertexArray.Dispose();
            extraLinesBuffer.Dispose();

            graphicsDevice.Dispose();
        }


        public static float randomf(float max)
        {
            return (float)r.NextDouble() * max;
        }
        public static float randomf(float min, float max)
        {
            return (float)r.NextDouble() * (max - min) + min;
        }
        public static Color4b randomCol()
        {
            return new Color4b((byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256), 255);
        }
    }
}
