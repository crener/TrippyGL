﻿using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace TrippyGL
{
    /// <summary>
    /// An OpenGL array of 2D textures
    /// </summary>
    public class Texture2DArray : Texture
    {
        /// <summary>The width of this texture</summary>
        public int Width { get; private set; }

        /// <summary>The height of this texture</summary>
        public int Height { get; private set; }

        /// <summary>The number of array layers of this texture</summary>
        public int Depth { get; private set; }

        /// <summary>The amount of samples this texture has. Most common value is 0</summary>
        public int Samples { get; private set; }

        public Texture2DArray(GraphicsDevice graphicsDevice, int width, int height, int depth, int samples = 0, TextureImageFormat imageFormat = TextureImageFormat.Color4b)
            : base(graphicsDevice, samples == 0 ? TextureTarget.Texture2DArray : TextureTarget.Texture2DMultisampleArray, imageFormat)
        {
            this.Samples = samples;
            RecreateImage(width, height, depth); //this also binds the texture

            if (samples == 0)
            {
                GL.TexParameter(this.TextureType, TextureParameterName.TextureMinFilter, (int)DefaultMinFilter);
                GL.TexParameter(this.TextureType, TextureParameterName.TextureMagFilter, (int)DefaultMagFilter);
            }
        }

        public void SetData<T>(T[] data, int dataOffset, int rectX, int rectY, int rectZ, int rectWidth, int rectHeight, int rectDepth) where T : struct
        {
            ValidateSetOperation(data, dataOffset, rectX, rectY, rectZ, rectWidth, rectHeight, rectDepth);

            GraphicsDevice.BindTexture(this);
            GL.TexSubImage3D(this.TextureType, 0, rectX, rectY, rectZ, rectWidth, rectHeight, rectDepth, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, this.PixelType, ref data[dataOffset]);
        }

        /// <summary>
        /// Sets the texture coordinate wrapping modes for when a texture is sampled outside the [0, 1] range
        /// </summary>
        /// <param name="sWrapMode">The wrap mode for the S (or texture-X) coordinate</param>
        /// <param name="tWrapMode">The wrap mode for the T (or texture-Y) coordinate</param>
        public void SetWrapModes(TextureWrapMode sWrapMode, TextureWrapMode tWrapMode)
        {
            if (this.Samples != 0)
                throw new InvalidOperationException("You can't change a multisampled texture's sampler states");

            GraphicsDevice.BindTextureSetActive(this);
            GL.TexParameter(TextureType, TextureParameterName.TextureWrapS, (int)sWrapMode);
            GL.TexParameter(TextureType, TextureParameterName.TextureWrapT, (int)tWrapMode);
        }

        /// <summary>
        /// Recreates this texture's image with a new size, resizing the texture but losing the image data
        /// </summary>
        /// <param name="width">The new width for the texture</param>
        /// <param name="height">The new height for the texture</param>
        /// <param name="depth">The new depth for the texture</param>
        public void RecreateImage(int width, int height, int depth)
        {
            ValidateTextureSize(width, height, depth);

            this.Width = width;
            this.Height = height;
            this.Depth = depth;

            GraphicsDevice.BindTextureSetActive(this);
            if (this.Samples == 0)
                GL.TexImage3D(this.TextureType, 0, this.PixelFormat, width, height, depth, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, this.PixelType, IntPtr.Zero);
            else
                GL.TexImage3DMultisample((TextureTargetMultisample)this.TextureType, this.Samples, this.PixelFormat, width, height, depth, true);
        }


        private protected void ValidateTextureSize(int width, int height, int depth)
        {
            if (width <= 0 || width > GraphicsDevice.MaxTextureSize)
                throw new ArgumentOutOfRangeException("width", width, "Texture width must be in the range (0, MAX_TEXTURE_SIZE]");

            if (height <= 0 || height > GraphicsDevice.MaxTextureSize)
                throw new ArgumentOutOfRangeException("height", height, "Texture height must be in the range (0, MAX_TEXTURE_SIZE]");

            if (depth <= 0 || depth > GraphicsDevice.MaxArrayTextureLayers)
                throw new ArgumentOutOfRangeException("depth", depth, "Texture depth must be in the range (0, MAX_ARRAY_TEXTURE_LAYERS)");
        }

        private protected void ValidateRectOperation(int rectX, int rectY, int rectZ, int rectWidth, int rectHeight, int rectDepth)
        {
            if (rectX < 0 || rectY >= this.Height)
                throw new ArgumentOutOfRangeException("rectX", rectX, "rectX must be in the range [0, this.Width)");

            if (rectY < 0 || rectY >= this.Height)
                throw new ArgumentOutOfRangeException("rectY", rectY, "rectY must be in the range [0, this.Height)");

            if (rectZ < 0 || rectZ >= this.Depth)
                throw new ArgumentOutOfRangeException("rectZ", rectZ, "rectZ must be in the range [0, this.Depth)");

            if (rectWidth <= 0 || rectHeight <= 0 || rectDepth <= 0)
                throw new ArgumentOutOfRangeException("rectWidth, rectHeight and rectDepth must be greater than 0");

            if (rectWidth > this.Width - rectX)
                throw new ArgumentOutOfRangeException("rectWidth", rectWidth, "rectWidth is too large");

            if (rectHeight > this.Height - rectY)
                throw new ArgumentOutOfRangeException("rectHeight", rectHeight, "rectHeight is too large");

            if (rectDepth > this.Depth - rectZ)
                throw new ArgumentOutOfRangeException("rectDepth", rectDepth, "rectDepth is too large");
        }

        private protected void ValidateSetOperation<T>(T[] data, int dataOffset, int rectX, int rectY, int rectZ, int rectWidth, int rectHeight, int rectDepth) where T : struct
        {
            if (this.Samples != 0)
                throw new InvalidOperationException("You can't write the data of a multisampled texture");

            //if (data == null) //it's gonna throw null reference anyway
            //    throw new ArgumentNullException("data", "Data array can't be null");

            if (dataOffset < 0 || dataOffset >= data.Length)
                throw new ArgumentOutOfRangeException("dataOffset", "dataOffset must be in the range [0, data.Length)");

            ValidateRectOperation(rectX, rectY, rectZ, rectWidth, rectHeight, rectDepth);

            if (data.Length - dataOffset < rectWidth * rectHeight * rectDepth)
                throw new ArgumentException("The data array isn't big enough to read the specified amount of data", "data");
        }

        private protected void ValidateGetOperation<T>(T[] data, int dataOffset) where T : struct
        {
            if (Samples != 0)
                throw new InvalidOperationException("You can't read the data of a multisampled texture");

            //if (data == null) //it's gonna throw null reference anyway
            //    throw new ArgumentNullException("data", "Data array can't be null");

            if (dataOffset < 0 || dataOffset >= data.Length)
                throw new ArgumentOutOfRangeException("dataOffset", "dataOffset must be in the range [0, data.Length)");

            if (data.Length - dataOffset < this.Width * this.Height * this.Depth)
                throw new ArgumentException("The provided data array isn't big enough for the texture starting from dataOffset", "data");
        }
    }
}
