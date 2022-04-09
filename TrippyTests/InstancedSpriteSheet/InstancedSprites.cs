using System;
using System.Diagnostics;
using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;
using TrippyGL.ImageSharp;
using TrippyTestBase;
using Rectangle = System.Drawing.Rectangle;

namespace InstancedSpriteSheet;

public class InstancedSprites : TestBase
{
    public const int GridWidth = 1000;
    public const int GridHeight = 1000;
    public const int TotalSprites = GridWidth * GridHeight;

    private SimpleShaderProgram shaderProgram;
    private SpriteSheetTextureBatcher m_textureBatcher;
    private Texture2D m_coinTexture;
    private Vector2[] m_positions;
    private int[] m_indexes;
    private Rectangle[] m_rects;
    
    
    private const int sprites = 14;
    private const int spiteSizeX = 64;
    private const int spiteSizeY = 64;
    private const int spacing = spiteSizeX / 3;
    
    public InstancedSprites(int preferredDepthBufferBits = 0)
        : base(nameof(InstancedSprites), preferredDepthBufferBits)
    {
        
    }

    protected override void OnLoad()
    {
        m_coinTexture = Texture2DExtensions.FromFile(graphicsDevice, "coins.png");

        Random rand = new Random();
        m_indexes = new int[TotalSprites];
        m_positions = new Vector2[TotalSprites];
        for (int x = 0; x < GridWidth; x++)
        for (int y = 0; y < GridHeight; y++)
        {
            m_positions[(y * GridWidth) + x] = new Vector2(x * spacing, y * spacing);
            m_indexes[(y * GridWidth) + x] = rand.Next(sprites);
        }

        uint bufferSize = (uint)TotalSprites;
        //uint bufferSize = 10000;
        
        graphicsDevice.BlendState = BlendState.NonPremultiplied;
        graphicsDevice.DepthState = DepthState.None;
        
        m_textureBatcher = new SpriteSheetTextureBatcher(graphicsDevice, m_coinTexture, bufferSize);
        shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(graphicsDevice, 0, 0, true);
        m_textureBatcher.SetShaderProgram(shaderProgram);

        m_rects = new Rectangle[sprites];
        for (int i = 0; i < sprites; i++)
        {
            m_rects[i] = new Rectangle(
                x: spiteSizeX * i,
                y: spiteSizeY * i,
                width: spiteSizeX,
                height: spiteSizeY);
        }
    }

    private double totalFrameTime = 0f;
    private Stopwatch stopwatch = new Stopwatch();
    protected override void OnRender(double dt)
    {
        stopwatch.Start();
        
        graphicsDevice.Clear(ClearBuffers.Color);
        m_textureBatcher.Begin(BatcherBeginMode.OnTheFly);

        totalFrameTime += dt * 15;
        int index = (int)(totalFrameTime % sprites);

        for (int i = 0; i < m_positions.Length; i++)
        { 
            m_textureBatcher.Draw(m_positions[i], m_rects[(index + m_indexes[i]) % sprites]);
        }
        
        m_textureBatcher.End();
        
        stopwatch.Stop();
        Console.WriteLine($"Render Duration: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Reset();
    }

    protected override void OnResized(Vector2D<int> size)
    {
        if (size.X == 0 || size.Y == 0)
            return;

        graphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);

        shaderProgram.Projection = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, 0, 1);
    }

    protected override void OnUnload()
    {
        m_textureBatcher.Dispose();
        m_coinTexture.Dispose();
        shaderProgram.Dispose();
    }
}