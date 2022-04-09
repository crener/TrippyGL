using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace TrippyGL
{
    /// <summary>
    /// Used internally by <see cref="TextureBatcher"/> to store the vertices for each Draw().
    /// </summary>
    internal sealed class TextureBatchItem
    {
        /// <summary>The top-left vertex.</summary>
        public VertexColorTexture VertexTL;
        /// <summary>The top-right vertex.</summary>
        public VertexColorTexture VertexTR;
        /// <summary>The bottom-left vertex.</summary>
        public VertexColorTexture VertexBL;
        /// <summary>The bottom-right vertex.</summary>
        public VertexColorTexture VertexBR;
        
        
        float widthf, heightf;
        int width, height;

        public TextureBatchItem(Texture2D texture)
        {
            widthf = (float)texture.Width;
            width = (int)texture.Width;
            heightf = (float)texture.Height;
            height = (int)texture.Height;
            
            SetColor(Color4b.White);
        }

        /// <summary>
        /// Calculates and sets all the values in this <see cref="TextureBatchItem"/> except for <see cref="SortValue"/>.
        /// </summary>
        public void SetValue(Vector2 position, Rectangle source, Color4b color, Vector2 scale, float rotation, Vector2 origin, float depth)
        {
            float tlx = -origin.X * scale.X;
            float tly = -origin.Y * scale.Y;
            float trx = tlx + source.Width * scale.X;
            float bly = tly + source.Height * scale.Y;

            float sin = MathF.Sin(rotation);
            float cos = MathF.Cos(rotation);
            // Positions
            VertexTL.Position.X = cos * tlx - sin * tly + position.X; 
            VertexTL.Position.Y = sin * tlx + cos * tly + position.Y; 
            VertexTL.Position.Z = depth;
            
            VertexTR.Position.X = cos * trx - sin * tly + position.X; 
            VertexTR.Position.Y = sin * trx + cos * tly + position.Y; 
            VertexTR.Position.Z = depth;
            
            VertexBL.Position.X = cos * tlx - sin * bly + position.X; 
            VertexBL.Position.Y = sin * tlx + cos * bly + position.Y; 
            VertexBL.Position.Z = depth;
            
            VertexBR.Position.X = cos * trx - sin * bly + position.X; 
            VertexBR.Position.Y = sin * trx + cos * bly + position.Y; 
            VertexBR.Position.Z = depth;

            // Textures
            SetRect(source.X, source.Right, source.Y, source.Bottom);
            SetColor(color);
        }

        /// <summary>
        /// Calculates and sets all the values in this <see cref="TextureBatchItem"/> except for <see cref="SortValue"/>
        /// without calculating rotation.
        /// </summary>
        public void SetValue(Vector2 position, Rectangle source, Color4b color, Vector2 scale, Vector2 origin, float depth)
        {
            Vector2 tl = position - origin * scale;
            Vector2 br = tl + new Vector2(source.Width, source.Height) * scale;
            VertexTL.Position = new Vector3(tl, depth);
            VertexTR.Position = new Vector3(br.X, tl.Y, depth);
            VertexBL.Position = new Vector3(tl.X, br.Y, depth);
            VertexBR.Position = new Vector3(br, depth);

            SetRect(source.X, source.Right, source.Y, source.Bottom);
            SetColor(color);
        }

        /// <summary>
        /// Calculates and sets all the values in this <see cref="TextureBatchItem"/> except for <see cref="SortValue"/>
        /// without calculating rotation.
        /// </summary>
        public void SetValue(Vector2 position, Color4b color, Vector2 scale, Vector2 origin, float depth)
        {
            Vector2 tl = position - origin * scale;
            Vector2 br = tl + new Vector2(width, height) * scale;
            VertexTL.Position = new Vector3(tl, depth);
            VertexTR.Position = new Vector3(br.X, tl.Y, depth);
            VertexBL.Position = new Vector3(tl.X, br.Y, depth);
            VertexBR.Position = new Vector3(br, depth);

            SetRect(0, 0, height, width);
            SetColor(color);
        }

        /// <summary>
        /// Calculates and sets all the values on this <see cref="TextureBatchItem"/> except for <see cref="SortValue"/>
        /// without calculating rotation nor scale.
        /// </summary>
        public void SetValue(Vector2 position, Rectangle source, Color4b color, float depth)
        {
            VertexTL.Position.X = position.X;
            VertexTL.Position.Y = position.Y;
            VertexTL.Position.Z = depth;
            VertexTR.Position.X = position.X + source.Width;
            VertexTR.Position.Y = position.Y;
            VertexTR.Position.Z = depth;
            VertexBL.Position.X = position.X;
            VertexBL.Position.Y = position.Y + source.Height;
            VertexBL.Position.Z = depth;
            VertexBR.Position.X = position.X + source.Width;
            VertexBR.Position.Y = position.Y + source.Height;
            VertexBR.Position.Z = depth;

            SetRect(source.X, source.Right, source.Y, source.Bottom);
            SetColor(color);
        }
        
        public void SetValue(Vector2 position, Rectangle source, float depth)
        {
            VertexTL.Position.X = position.X;
            VertexTL.Position.Y = position.Y;
            VertexTL.Position.Z = depth;
            VertexTR.Position.X = position.X + source.Width;
            VertexTR.Position.Y = position.Y;
            VertexTR.Position.Z = depth;
            VertexBL.Position.X = position.X;
            VertexBL.Position.Y = position.Y + source.Height;
            VertexBL.Position.Z = depth;
            VertexBR.Position.X = position.X + source.Width;
            VertexBR.Position.Y = position.Y + source.Height;
            VertexBR.Position.Z = depth;

            SetRect(source.X, source.Right, source.Y, source.Bottom);
        }

        /// <summary>
        /// Calculates and sets all the values in this <see cref="TextureBatchItem"/> except for <see cref="SortValue"/>.
        /// </summary>
        public void SetValue(RectangleF destination, Rectangle source, Color4b color, float depth)
        {
            VertexTL.Position = new Vector3(destination.X, destination.Y, depth);
            VertexTR.Position = new Vector3(destination.Right, destination.Y, depth);
            VertexBL.Position = new Vector3(destination.X, destination.Bottom, depth);
            VertexBR.Position = new Vector3(destination.Right, destination.Bottom, depth);

            SetRect(source.X, source.Right, source.Y, source.Bottom);
            SetColor(color);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetRect(float left, float right, float top, float bottom)
        {
            VertexTL.TexCoords.X = left / widthf;
            VertexTL.TexCoords.Y = top / heightf;
            VertexBR.TexCoords.X = right / widthf;
            VertexBR.TexCoords.Y = bottom / heightf;
            VertexTR.TexCoords.X = VertexBR.TexCoords.X;
            VertexTR.TexCoords.Y = VertexTL.TexCoords.Y;
            VertexBL.TexCoords.X = VertexTL.TexCoords.X;
            VertexBL.TexCoords.Y = VertexBR.TexCoords.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetColor(Color4b color)
        {

            VertexTL.Color = color;
            VertexTR.Color = color;
            VertexBL.Color = color;
            VertexBR.Color = color;
        }

        public override string ToString()
        {
            return
                //$"Texture.Handle={Texture.Handle}, " +
                $"Texture.Handle=null, " +
                $"VertexTL.Position=({VertexTL.Position.X},{VertexTL.Position.Y},{VertexTL.Position.Z})";
        }
    }
}
