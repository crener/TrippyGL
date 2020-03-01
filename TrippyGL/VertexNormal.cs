using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices;

namespace TrippyGL
{
    /// <summary>
    /// Represents a vertex with a <see cref="Vector3"/> Position and <see cref="Vector3"/> Normal.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexNormal : IVertex
    {
        /// <summary>The size of a <see cref="VertexNormal"/> measured in bytes.</summary>
        public const int SizeInBytes = (3 + 3) * 4;

        /// <summary>The vertex's position.</summary>
        public Vector3 Position;

        /// <summary>The vertex's normal.</summary>
        public Vector3 Normal;

        /// <summary>
        /// Creates a <see cref="VertexNormal"/> with the specified position and normal.
        /// </summary>
        public VertexNormal(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        public override string ToString()
        {
            return string.Concat("(", Position.X.ToString(), ", ", Position.Y.ToString(), ", ", Position.Z.ToString(), ") (", Normal.X.ToString(), ", ", Normal.Y.ToString(), ", ", Normal.Z.ToString(), ")");
        }

        public int AttribDescriptionCount => 2;

        public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
        {
            descriptions[0] = new VertexAttribDescription(ActiveAttribType.FloatVec3);
            descriptions[1] = new VertexAttribDescription(ActiveAttribType.FloatVec3);

        }
    }
}
