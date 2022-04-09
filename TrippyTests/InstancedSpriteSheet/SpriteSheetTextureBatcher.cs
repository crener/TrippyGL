using System;
using System.Drawing;
using System.Net.Mime;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using TrippyGL.Utils;

namespace TrippyGL
{
    /// <summary>
    /// Provides a simple and efficient way to draw 2D textures in batches.
    /// </summary>
    public sealed class SpriteSheetTextureBatcher : IDisposable
    {
        /// <summary>The initial capacity for the internal batch items array.</summary>
        public const uint InitialBatchItemsCapacity = 256;

        /// <summary>The maximum capacity for the internal batch items array.</summary>
        public const uint MaxBatchItemCapacity = int.MaxValue;

        /// <summary>The initial capacity for the <see cref="VertexBuffer{T}"/> used for drawing the item's vertices.</summary>
        private const uint InitialBufferCapacity = InitialBatchItemsCapacity * 3;

        /// <summary>The maximum capacity for the <see cref="VertexBuffer{T}"/> used for drawing the item's vertices.</summary>
        private const uint MaxBufferCapacity = 32768;

        /// <summary>
        /// The <see cref="TrippyGL.GraphicsDevice"/> with which this <see cref="SpriteSheetTextureBatcher"/> was created.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>Used to store vertices before sending them to <see cref="vertexBuffer"/>.</summary>
        private VertexColorTexture[] triangles;

        /// <summary>Stores the triangles for rendering.</summary>
        private readonly VertexBuffer<VertexColorTexture> vertexBuffer;

        /// <summary>Stores the batch items that haven't been flushed yet.</summary>
        private TextureBatchItem[] batchItems;
        /// <summary>The amount of batch items stored in <see cref="batchItems"/>.</summary>
        private int batchItemCount;

        /// <summary>
        /// Whether <see cref="Begin(BatcherBeginMode)"/> was called on this <see cref="SpriteSheetTextureBatcher"/>
        /// but <see cref="End"/> hasn't yet.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// The <see cref="BatcherBeginMode"/> specified in the last <see cref="Begin(BatcherBeginMode)"/>.
        /// </summary>
        public BatcherBeginMode BeginMode { get; private set; }

        /// <summary>The <see cref="ShaderProgram"/> this <see cref="SpriteSheetTextureBatcher"/> is currently using.</summary>
        public ShaderProgram ShaderProgram { get; private set; }

        /// <summary>
        /// The <see cref="ShaderUniform"/> this <see cref="SpriteSheetTextureBatcher"/> uses for setting the texture
        /// on <see cref="ShaderProgram"/>.
        /// </summary>
        public ShaderUniform TextureUniform { get; private set; }

        /// <summary>Whether this <see cref="SpriteSheetTextureBatcher"/> has been disposed.</summary>
        public bool IsDisposed => vertexBuffer.IsDisposed;

        public Texture2D Texture { get; }
        
        /// <summary>
        /// Creates a <see cref="SpriteSheetTextureBatcher"/>, with a specified initial capacity for batch items as
        /// optional parameter.
        /// </summary>
        /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> this <see cref="SpriteSheetTextureBatcher"/> will use.</param>
        /// <param name="initialBatchCapacity">The initial capacity for the internal batch items array.</param>
        public SpriteSheetTextureBatcher(GraphicsDevice graphicsDevice, Texture2D texture, uint initialBatchCapacity = InitialBatchItemsCapacity)
        {
            if (initialBatchCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialBatchCapacity), nameof(initialBatchCapacity) + " must be greater than 0.");

            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            vertexBuffer = new VertexBuffer<VertexColorTexture>(graphicsDevice, InitialBufferCapacity, BufferUsage.StreamDraw);
            Texture = texture;
            
            batchItems = new TextureBatchItem[initialBatchCapacity];
            for (int i = 0; i < batchItems.Length; i++)
                batchItems[i] = new TextureBatchItem(Texture);
            
            batchItemCount = 0;
            IsActive = false;
        }

        /// <summary>
        /// Sets the <see cref="TrippyGL.ShaderProgram"/> this <see cref="SpriteSheetTextureBatcher"/> uses for rendering.
        /// </summary>
        /// <param name="simpleProgram">The <see cref="SimpleShaderProgram"/> to use.</param>
        /// <remarks>
        /// The <see cref="SimpleShaderProgram"/> doesn't need to have texture sampling and vertex colors
        /// enabled. Lighting however will not work, since the vertices lack normal data.<para/>
        /// The locations of the attributes on the program must still match 0 for position, 1 for
        /// color and 2 for texture coordinates.
        /// </remarks>
        public void SetShaderProgram(SimpleShaderProgram simpleProgram)
        {
            SetShaderProgram(simpleProgram, (simpleProgram.TextureEnabled) ? simpleProgram.sampUniform : default);
        }

        /// <summary>
        /// Sets the <see cref="TrippyGL.ShaderProgram"/> this <see cref="SpriteSheetTextureBatcher"/> uses for rendering.
        /// </summary>
        /// <param name="shaderProgram">The <see cref="TrippyGL.ShaderProgram"/> to use.</param>
        /// <param name="textureUniform">The <see cref="ShaderUniform"/> from which to set the textures to draw.</param>
        /// <remarks>
        /// The <see cref="TrippyGL.ShaderProgram"/> must use attribute location 0 for position.
        /// Color and TexCoords are optional, but if present they must be in attribute locations
        /// 1 and 2 respectively.<para/>
        /// textureUniform can be an empty <see cref="ShaderUniform"/>, in which case the <see cref="SpriteSheetTextureBatcher"/>
        /// will simply not set any texture when rendering.
        /// </remarks>
        public void SetShaderProgram(ShaderProgram shaderProgram, ShaderUniform textureUniform)
        {
            if (IsActive)
                throw new InvalidOperationException(nameof(ShaderProgram) + " cant be changed while the " + nameof(SpriteSheetTextureBatcher) + " is active.");
            

            if (shaderProgram.GraphicsDevice != GraphicsDevice)
                throw new ArgumentException(nameof(ShaderProgram) + " must belong to the same " + nameof(GraphicsDevice)
                    + " this " + nameof(SpriteSheetTextureBatcher) + " was created with.", nameof(shaderProgram));

            // If textureUniform isn't empty, we check that it's valid.
            if (!textureUniform.IsEmpty)
            {
                if (textureUniform.OwnerProgram != shaderProgram)
                    throw new ArgumentException(nameof(textureUniform) + " must belong to the provided " + nameof(ShaderProgram) + ".", nameof(textureUniform));

                if (!TrippyUtils.IsUniformSampler2DType(textureUniform.UniformType))
                    throw new ArgumentException("The provided " + nameof(ShaderUniform) + " must be a Sampler2D type.", nameof(textureUniform));
            }

            // We check that the ShaderProgram has an attribute in location 0 and is a FloatVec3.
            if (!shaderProgram.TryFindAttributeByLocation(0, out ActiveVertexAttrib attrib) || attrib.AttribType != AttributeType.FloatVec3)
                throw new ArgumentException("The shader program's attribute at location 0 must be of type FloatVec3 (used for position).", nameof(shaderProgram));

            // If the ShaderProgram has an attribute in location 1, it has to be a FloatVec4.
            if (shaderProgram.TryFindAttributeByLocation(1, out attrib) && attrib.AttribType != AttributeType.FloatVec4)
                throw new ArgumentException("The shader program's attribute at location 1 must be of type FloatVec4 (used for color).", nameof(shaderProgram));

            // If the ShaderProgram has an attribute in location 2, it has to be a FloatVec2 used for TexCoords.
            if (shaderProgram.TryFindAttributeByLocation(2, out attrib) && attrib.AttribType != AttributeType.FloatVec2)
                throw new ArgumentException("The shader program's attribute at location 2 must be of type FloatVec2 (used for texture coordinates).", nameof(shaderProgram));

            // Everything's valid, let's store the new ShaderProgram and ShaderUniform.
            ShaderProgram = shaderProgram;
            TextureUniform = textureUniform;
        }

        /// <summary>
        /// Begins drawing a new batch of textures.
        /// </summary>
        /// <param name="beginMode">The mode in which flushing the textures is handled.</param>
        public void Begin(BatcherBeginMode beginMode = BatcherBeginMode.Deferred)
        {
            if (vertexBuffer.IsDisposed)
                throw new ObjectDisposedException(nameof(SpriteSheetTextureBatcher));

            if (!Enum.IsDefined(typeof(BatcherBeginMode), beginMode))
                throw new ArgumentException("Invalid " + nameof(BatcherBeginMode) + " value.", nameof(beginMode));

            if (ShaderProgram == null)
                throw new InvalidOperationException("A " + nameof(ShaderProgram) + " must be specified (via " + nameof(SetShaderProgram) + "()) before using Begin().");

            if (IsActive)
                throw new InvalidOperationException("This " + nameof(SpriteSheetTextureBatcher) + " has already begun.");

            batchItemCount = 0;
            BeginMode = beginMode;
            IsActive = true;
        }

        /// <summary>
        /// Ends drawing a batch of textures and flushes any textures that are waiting to be drawn.
        /// </summary>
        public void End()
        {
            if (!IsActive)
                throw new InvalidOperationException("Begin() must be called before End().");

            // We flush. We can ensure all the items have the same texture if BeginMode is Immediate or OnTheFly.
            Flush();

            IsActive = false;
        }

        /// <summary>
        /// Throws an exception if this <see cref="SpriteSheetTextureBatcher"/> isn't currently active.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateBeginCalled()
        {
            if (!IsActive)
                throw new InvalidOperationException("Draw() must be called in between Begin() and End().");
        }

        /// <summary>
        /// Ensures that <see cref="batchItems"/> has at least the required capacity, but if the array
        /// is resized then the new capacity won't exceed <see cref="MaxBatchItemCapacity"/>.
        /// </summary>
        /// <param name="requiredCapacity">The required capacity for the <see cref="batchItems"/> array.</param>
        /// <returns>Whether the required capacity is met by the new capacity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureBatchListCapacity(int requiredCapacity)
        {
            int currentCapacity = batchItems.Length;
            if(currentCapacity == MaxBatchItemCapacity)
                return false; // don't drop render calls

            if (currentCapacity < requiredCapacity)
            {
                int newSize = Math.Min(TrippyMath.GetNextCapacity(currentCapacity, requiredCapacity), (int)MaxBatchItemCapacity);

                if(newSize > batchItems.Length)
                {
                    // We resize the batchItems array and fill the new elements with new TextureBatchItem
                    // instances, so we never have a null inside the batchItems array.
                    Array.Resize(ref batchItems, newSize);
                    for (int i = currentCapacity; i < batchItems.Length; i++)
                        batchItems[i] = new TextureBatchItem(Texture);
                }
            }
            else
            {
                return true;
            }

            return requiredCapacity <= batchItems.Length;
        }

        /// <summary>
        /// Ensures that <see cref="vertexBuffer"/> and the <see cref="triangles"/> array have at least
        /// the required capacity, but if a resize is needed then the new capacity won't exceed
        /// <see cref="MaxBufferCapacity"/>.
        /// </summary>
        /// <param name="requiredCapacity">The required capacity for <see cref="vertexBuffer"/>.</param>
        /// <remarks>
        /// <see cref="triangles"/> will always be resized to have the same size as <see cref="vertexBuffer"/>.
        /// </remarks>
        private void EnsureBufferCapacity(int requiredCapacity)
        {
            uint currentCapacity = vertexBuffer.StorageLength;
            if (currentCapacity == MaxBufferCapacity)
                return;

            if(currentCapacity < requiredCapacity)
            {
                vertexBuffer.RecreateStorage(Math.Min((uint)TrippyMath.GetNextCapacity((int)currentCapacity, requiredCapacity), MaxBufferCapacity));
                triangles = new VertexColorTexture[vertexBuffer.StorageLength];
            }
        }

        /// <summary>
        /// Gets a <see cref="TextureBatchItem"/> that's already in the <see cref="batchItems"/> array,
        /// in the next available position, then increments <see cref="batchItemCount"/>.
        /// </summary>
        /// <remarks>
        /// When a <see cref="TextureBatchItem"/> is returned by this method, it is already inside
        /// the <see cref="batchItems"/> array. To properly use this method, get an item and simply
        /// set it's value, without storing the item anywhere.
        /// </remarks>
        private TextureBatchItem GetNextBatchItem()
        {
            // We check that we have enough capacity for one more batch item.
            if (!EnsureBatchListCapacity(batchItemCount + 1))
            {
                // If we don't and the array can't be expanded any further, we try to flush.
                // Flushing can only occur before End() if BeginMode is OnTheFly or Immediate.
                // If BeginMode is one of these, we can also ensure that all the batch items share a texture.
                if (BeginMode == BatcherBeginMode.OnTheFly || BeginMode == BatcherBeginMode.Immediate)
                    Flush();
                else
                    throw new InvalidOperationException("Too many " + nameof(SpriteSheetTextureBatcher) + " items. Try drawing less per Begin()-End() cycle or use OnTheFly or Immediate begin modes.");
            }

            // We are ensured the elements in batchItems are never null, so let's just return the next one.
            return batchItems[batchItemCount++];
        }

        /// <summary>
        /// Adds a <see cref="Texture2D"/> for drawing to the current batch.
        /// </summary>
        /// <param name="position">The position at which to draw the texture.</param>
        /// <param name="source">The area of the texture to draw (or null to draw the whole texture).</param>
        /// <param name="color">The color tint of the sprite</param>
        /// <param name="depth">The depth at which to draw the texture.</param>
        public void Draw(Vector2 position, Rectangle source, Color4b color, float depth = 0)
        {
            ValidateBeginCalled();

            TextureBatchItem item = GetNextBatchItem();
            item.SetValue(position, source, color, depth);

            FlushIfNeeded();
        }

        /// <summary>
        /// Adds a <see cref="Texture2D"/> for drawing to the current batch.
        /// </summary>
        /// <param name="position">The position at which to draw the texture.</param>
        /// <param name="source">The area of the texture to draw (or null to draw the whole texture).</param>
        /// <param name="depth">The depth at which to draw the texture.</param>
        public void Draw(Vector2 position, Rectangle source, float depth = 0)
        {
            ValidateBeginCalled();

            TextureBatchItem item = GetNextBatchItem();
            item.SetValue(position, source, depth);

            FlushIfNeeded();
        }

        /// <summary>
        /// Checks whether the <see cref="SpriteSheetTextureBatcher"/> should be flushed after adding more batch
        /// items based on the current <see cref="BeginMode"/> and if so, flushes.
        /// </summary>
        /// <returns>Whether the <see cref="SpriteSheetTextureBatcher"/> was flushed.</returns>
        /// <remarks>This should always be called after adding batch items of the same texture.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FlushIfNeeded()
        {
            if (BeginMode == BatcherBeginMode.Immediate)
            {
                Flush();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Renders all the items in <see cref="batchItems"/>.
        /// </summary>
        /// <remarks>
        /// This function assumes <see cref="Begin(BatcherBeginMode)"/> was succesfully called and
        /// that this object isn't disposed.
        /// </remarks>
        private void Flush()
        {
            if (batchItemCount == 0)
                return;

            // In order to know whether we need to sort first, we can AND the BeginMode with 1.
            if ((BeginMode & (BatcherBeginMode)1) == (BatcherBeginMode)1)
                Array.Sort(batchItems, 0, batchItemCount);

            // We set the vertex buffer and shader program onto the GraphicsDevice so we can use them.
            GraphicsDevice.VertexArray = vertexBuffer;
            GraphicsDevice.ShaderProgram = ShaderProgram;

            // We now need to draw all the textures, in the order in which they are in the array.
            // Since we can only draw with one texture per draw call, we'll have split the items
            // into batches that use the same texture.

            // We do this in the outer while loop, which keeps going while we have more items to draw.

            
            if (!TextureUniform.IsEmpty)
                TextureUniform.SetValueTexture(Texture);

            int itemStartIndex = 0;
            while (itemStartIndex < batchItemCount)
            {
                // We search for the end of this batch. That is, up to what item can we draw with this texture.
                int itemEndIndex = batchItemCount;
                // We make a Span<TextureBatchItem> containing all the items to draw in this batch.
                Span<TextureBatchItem> items = batchItems.AsSpan(itemStartIndex, itemEndIndex - itemStartIndex);

                // The next cycle of this loop should start drawing items where this cycle left off.
                itemStartIndex = itemEndIndex;

                // We ensure that the buffers have enough capacity, or as much as they can have.
                const int vertCountPerSprite = 6;
                EnsureBufferCapacity(items.Length * vertCountPerSprite);
                // We calculate the maximum amount of batch items that we'll be able to draw per draw call.
                int batchItemsPerDrawCall = (int)vertexBuffer.StorageLength / vertCountPerSprite;
                Span<VertexColorTexture> triSpan = triangles.AsSpan();

                // Depending on how the constants are set up, we might be able to draw all the items
                // in a single call, or we might need to further split them up if they can't all fit
                // together into the buffer.
                for (int startIndex = 0; startIndex < items.Length; startIndex += batchItemsPerDrawCall)
                {
                    // We calculate up to which item we can draw in this draw call.
                    int endIndex = Math.Min(startIndex + batchItemsPerDrawCall, items.Length);

                    // We make the triangles for all the items and add all those vertices to
                    // the triangles array. Each item uses two triangles, or six vertices.
                    int triangleIndex = 0;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        TextureBatchItem item = items[i];
                        triSpan[triangleIndex++] = item.VertexTL;
                        triSpan[triangleIndex++] = item.VertexBR;
                        triSpan[triangleIndex++] = item.VertexTR;

                        triSpan[triangleIndex++] = item.VertexTL;
                        triSpan[triangleIndex++] = item.VertexBL;
                        triSpan[triangleIndex++] = item.VertexBR;
                    }

                    // We copy the vertices over to the vertexBuffer and draw them.
                    vertexBuffer.DataSubset.SetData(triangles.AsSpan(0, triangleIndex));
                    GraphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, (uint)triangleIndex);
                }
            }

            // We reset batchItemCount to 0.
            batchItemCount = 0;
        }

        /// <summary>
        /// Disposes the <see cref="GraphicsResource"/>-s used by this <see cref="SpriteSheetTextureBatcher"/>.
        /// </summary>
        public void Dispose()
        {
            vertexBuffer.Dispose();
        }
    }
}