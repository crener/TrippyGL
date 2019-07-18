﻿using System;
using System.IO;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using TrippyGL;

namespace TrippyTesting.Tests
{
    class InstancedRendering : GameWindow
    {
        const int MaxParticles = 50;

        System.Diagnostics.Stopwatch stopwatch;
        public Random r = new Random();
        public float time, deltaTime;

        BufferObject ptcBuffer;
        VertexDataBufferSubset<Matrix4> matSubset;
        VertexDataBufferSubset<VertexColor> vertexSubset;
        VertexArray ptcArray;

        ShaderProgram ptcProgram;

        GraphicsDevice graphicsDevice;

        MouseState ms, oldMs;
        KeyboardState ks, oldKs;

        public InstancedRendering() : base(1280, 720, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 32, 0, 0, ColorFormat.Empty, 2), "haha yes", GameWindowFlags.Default, DisplayDevice.Default, 4, 0, GraphicsContextFlags.Default)
        {
            VSync = VSyncMode.On;
            graphicsDevice = new GraphicsDevice(Context);
            graphicsDevice.DebugMessagingEnabled = true;
            graphicsDevice.DebugMessage += Program.OnDebugMessage;

            Console.WriteLine(String.Concat("GL Version: ", graphicsDevice.GLMajorVersion, ".", graphicsDevice.GLMinorVersion));
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
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            time = 0;

            const int maxvert = 128;
            VertexColor[] vertices = new VertexColor[maxvert+1];
            vertices[0] = new VertexColor(new Vector3(0, 0, 0), randomCol());
            for (int i = 0; i < maxvert; i++)
            {
                float rot = i * MathHelper.TwoPi / maxvert;
                float scale = (i * 10f / maxvert) % 0.5f + 0.5f;
                scale = 3f * scale * scale - 2f * scale * scale * scale;
                vertices[i+1] = new VertexColor(new Vector3((float)Math.Cos(rot) * scale, (float)Math.Sin(rot) * scale, 0f), randomCol());
            }

            ptcBuffer = new BufferObject(graphicsDevice, MaxParticles * 64 + VertexColor.SizeInBytes * vertices.Length, BufferUsageHint.StaticDraw);
            matSubset = new VertexDataBufferSubset<Matrix4>(ptcBuffer, 0, MaxParticles);
            vertexSubset = new VertexDataBufferSubset<VertexColor>(ptcBuffer, vertices, 0, matSubset.StorageNextInBytes, vertices.Length);

            ptcArray = new VertexArray(graphicsDevice, new VertexAttribSource[]
            {
                new VertexAttribSource(matSubset, ActiveAttribType.FloatMat4, 1),
                new VertexAttribSource(vertexSubset, ActiveAttribType.FloatVec3),
                new VertexAttribSource(vertexSubset, ActiveAttribType.FloatVec4, true, VertexAttribPointerType.UnsignedByte)
            });

            ptcProgram = new ShaderProgram(graphicsDevice);
            ptcProgram.AddVertexShader(File.ReadAllText("instanced/vs.glsl"));
            ptcProgram.AddFragmentShader(File.ReadAllText("instanced/fs.glsl"));
            ptcProgram.SpecifyVertexAttribs(ptcArray.AttribSources, new string[] { "World", "vPosition", "vColor" });
            ptcProgram.LinkProgram();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            oldMs = ms;
            ms = Mouse.GetState();
            oldKs = ks;
            ks = Keyboard.GetState();

            float prevTime = time;
            time = (float)stopwatch.Elapsed.TotalSeconds;
            deltaTime = time - prevTime;
            ErrorCode c;
            while ((c = GL.GetError()) != ErrorCode.NoError)
            {
                Console.WriteLine("Error found: " + c);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthTestingEnabled = false;
            graphicsDevice.ClearColor = new Color4(0f, 0f, 0f, 1f);
            graphicsDevice.Framebuffer = null;

            graphicsDevice.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            graphicsDevice.VertexArray = ptcArray;
            graphicsDevice.ShaderProgram = ptcProgram;
            matSubset.SetData(new Matrix4[] { Matrix4.CreateScale(24f) * Matrix4.CreateRotationZ(time) *  Matrix4.CreateTranslation(64, 24, 0) });
            graphicsDevice.DrawArrays(PrimitiveType.TriangleFan, 0, vertexSubset.StorageLength);

            SwapBuffers();
        }

        protected override void OnResize(EventArgs e)
        {
            graphicsDevice.Viewport = new Rectangle(0, 0, this.Width, this.Height);

            float ratio = (float)this.Width / (float)this.Height;

            Matrix4 view = Matrix4.Identity;
            ptcProgram.Uniforms["View"].SetValueMat4(ref view);

            Matrix4 proj = Matrix4.CreateOrthographicOffCenter(0, 128, 0, 128f / ratio, 0, 1);
            ptcProgram.Uniforms["Projection"].SetValueMat4(ref proj);
        }

        protected override void OnUnload(EventArgs e)
        {


            graphicsDevice.Dispose();
        }

        private Color4b randomCol()
        {
            return new Color4b((byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256), 255);
        }
    }
}
