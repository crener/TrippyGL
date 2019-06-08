﻿using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace TrippyGL
{
    /// <summary>
    /// Contains various methods used throughout the library
    /// </summary>
    public static class TrippyUtils
    {
        /// <summary>
        /// Returns whether the specified data base type is of integer format (such as byte, ushort, int, uint)
        /// </summary>
        public static bool IsVertexAttribIntegerType(VertexAttribPointerType dataBaseType)
        {
            return dataBaseType == VertexAttribPointerType.UnsignedByte || dataBaseType == VertexAttribPointerType.Byte
                || dataBaseType == VertexAttribPointerType.UnsignedShort || dataBaseType == VertexAttribPointerType.Short
                || dataBaseType == VertexAttribPointerType.UnsignedInt || dataBaseType == VertexAttribPointerType.Int
                || dataBaseType == VertexAttribPointerType.Int2101010Rev || dataBaseType == VertexAttribPointerType.UnsignedInt2101010Rev
                || dataBaseType == VertexAttribPointerType.UnsignedInt10F11F11FRev;
        }

        /// <summary>
        /// Gets whether the specified attrib type is an integer type (such as int, ivecX, uint or uvecX)
        /// </summary>
        public static bool IsVertexAttribIntegerType(ActiveAttribType attribType)
        {
            return attribType == ActiveAttribType.Int || attribType == ActiveAttribType.UnsignedInt
                || (attribType >= ActiveAttribType.IntVec2 && attribType <= ActiveAttribType.IntVec4)
                || (attribType >= ActiveAttribType.UnsignedIntVec2 && attribType <= ActiveAttribType.UnsignedIntVec4);
        }

        /// <summary>
        /// Gets whether the specified attrib type is a floating point double type (such as double, dvecX or dmatMxN)
        /// </summary>
        public static bool IsVertexAttribDoubleType(ActiveAttribType attribType)
        {
            return attribType == ActiveAttribType.Double
                || (attribType >= ActiveAttribType.DoubleVec2 && attribType <= ActiveAttribType.DoubleVec4)
                || (attribType >= ActiveAttribType.DoubleMat2 && attribType <= ActiveAttribType.DoubleMat4x3);
        }

        /// <summary>
        /// Gets the corresponding variables for the specified ActiveAttribType
        /// </summary>
        /// <param name="attribType">The attribute type to query</param>
        /// <param name="indexUseCount">The amount of attribute indices it will need</param>
        /// <param name="size">The amount of components each index will have</param>
        /// <param name="type">The base type of each component</param>
        public static void GetVertexAttribTypeData(ActiveAttribType attribType, out int indexUseCount, out int size, out VertexAttribPointerType type)
        {
            indexUseCount = GetVertexAttribTypeIndexCount(attribType);
            size = GetVertexAttribTypeSize(attribType);
            type = GetVertexAttribBaseType(attribType);
        }

        /// <summary>
        /// Gets the base variable type for the specified attribute type
        /// (for example, vec4 would return float. dmat2 would return double, ivec2 returns int)
        /// </summary>
        public static VertexAttribPointerType GetVertexAttribBaseType(ActiveAttribType attribType)
        {
            if (attribType == ActiveAttribType.Float // is it a float?
                || (attribType >= ActiveAttribType.FloatVec2 && attribType <= ActiveAttribType.FloatVec4) // or is it a float vector?
                || (attribType >= ActiveAttribType.FloatMat2 && attribType <= ActiveAttribType.FloatMat4x3)) // or is it a float matrix?
                return VertexAttribPointerType.Float;

            if (attribType == ActiveAttribType.Int
                || (attribType >= ActiveAttribType.IntVec2 && attribType <= ActiveAttribType.IntVec4))
                return VertexAttribPointerType.Int;

            if (attribType == ActiveAttribType.Double
                || (attribType >= ActiveAttribType.DoubleVec2 && attribType <= ActiveAttribType.DoubleVec4)
                || (attribType >= ActiveAttribType.DoubleMat2 && attribType <= ActiveAttribType.DoubleMat4x3))
                return VertexAttribPointerType.Double;

            if (attribType == ActiveAttribType.UnsignedInt
                || (attribType >= ActiveAttribType.UnsignedIntVec2 || attribType <= ActiveAttribType.UnsignedIntVec4))
                return VertexAttribPointerType.UnsignedInt;

            throw new ArgumentException("The provided value is not a valid enum value", "attribType");
        }

        /// <summary>
        /// Gets the attribute's size. By size, this means "vector size" (float is 1, vec2i is 2, bvec4 is 4, etc)
        /// </summary>
        public static int GetVertexAttribTypeSize(ActiveAttribType attribType)
        {
            if ((attribType >= ActiveAttribType.Int && attribType <= ActiveAttribType.Float) || attribType == ActiveAttribType.Double)
                return 1;

            if (attribType >= ActiveAttribType.FloatVec2 && attribType <= ActiveAttribType.FloatVec4)
                return attribType - ActiveAttribType.FloatVec2 + 2;

            if (attribType >= ActiveAttribType.IntVec2 && attribType <= ActiveAttribType.IntVec4)
                return attribType - ActiveAttribType.IntVec2 + 2;

            if (attribType >= ActiveAttribType.UnsignedIntVec2 && attribType <= ActiveAttribType.UnsignedIntVec4)
                return attribType - ActiveAttribType.UnsignedIntVec2 + 2;

            if (attribType >= ActiveAttribType.DoubleVec2 && attribType <= ActiveAttribType.DoubleVec4)
                return attribType - ActiveAttribType.DoubleVec2 + 2;

            switch (attribType)
            {
                case ActiveAttribType.FloatMat2:
                case ActiveAttribType.FloatMat3x2:
                case ActiveAttribType.FloatMat4x2:
                case ActiveAttribType.DoubleMat2:
                case ActiveAttribType.DoubleMat3x2:
                case ActiveAttribType.DoubleMat4x2:
                    return 2;

                case ActiveAttribType.FloatMat3:
                case ActiveAttribType.FloatMat2x3:
                case ActiveAttribType.FloatMat4x3:
                case ActiveAttribType.DoubleMat3:
                case ActiveAttribType.DoubleMat2x3:
                case ActiveAttribType.DoubleMat4x3:
                    return 3;

                case ActiveAttribType.FloatMat4:
                case ActiveAttribType.FloatMat2x4:
                case ActiveAttribType.FloatMat3x4:
                case ActiveAttribType.DoubleMat4:
                case ActiveAttribType.DoubleMat2x4:
                case ActiveAttribType.DoubleMat3x4:
                    return 4;
            }

            throw new ArgumentException("The provided value is not a valid enum value", "attribType");
        }

        /// <summary>
        /// Gets the amount of indices the vertex attribute occupies
        /// </summary>
        public static int GetVertexAttribTypeIndexCount(ActiveAttribType attribType)
        {
            if ((attribType >= ActiveAttribType.Int && attribType <= ActiveAttribType.Float)
                || attribType == ActiveAttribType.Double
                || (attribType >= ActiveAttribType.FloatVec2 && attribType <= ActiveAttribType.IntVec4)
                || (attribType >= ActiveAttribType.UnsignedIntVec2 && attribType <= ActiveAttribType.UnsignedIntVec4)
                || (attribType >= ActiveAttribType.DoubleVec2 && attribType <= ActiveAttribType.DoubleVec4))
                return 1;

            switch (attribType)
            {
                case ActiveAttribType.FloatMat2:
                case ActiveAttribType.FloatMat2x3:
                case ActiveAttribType.FloatMat2x4:
                case ActiveAttribType.DoubleMat2:
                case ActiveAttribType.DoubleMat2x3:
                case ActiveAttribType.DoubleMat2x4:
                    return 2;

                case ActiveAttribType.FloatMat3:
                case ActiveAttribType.FloatMat3x2:
                case ActiveAttribType.FloatMat3x4:
                case ActiveAttribType.DoubleMat3:
                case ActiveAttribType.DoubleMat3x2:
                case ActiveAttribType.DoubleMat3x4:
                    return 3;

                case ActiveAttribType.FloatMat4:
                case ActiveAttribType.FloatMat4x2:
                case ActiveAttribType.FloatMat4x3:
                case ActiveAttribType.DoubleMat4:
                case ActiveAttribType.DoubleMat4x2:
                case ActiveAttribType.DoubleMat4x3:
                    return 4;
            }

            throw new ArgumentException("The provided value is not a valid enum value", "attribType");
        }

        /// <summary>
        /// Gets the size in bytes of an attribute type
        /// </summary>
        public static int GetVertexAttribSizeInBytes(VertexAttribPointerType type)
        {
            switch (type)
            {
                case VertexAttribPointerType.Byte:
                case VertexAttribPointerType.UnsignedByte:
                    return 1;

                case VertexAttribPointerType.Short:
                case VertexAttribPointerType.UnsignedShort:
                case VertexAttribPointerType.HalfFloat:
                    return 2;

                case VertexAttribPointerType.Float:
                case VertexAttribPointerType.Int:
                case VertexAttribPointerType.UnsignedInt:
                case VertexAttribPointerType.Fixed:
                    return 4;

                case VertexAttribPointerType.Double:
                    return 8;

                //case VertexAttribPointerType.Int2101010Rev:
                //case VertexAttribPointerType.UnsignedInt10F11F11FRev:
                //case VertexAttribPointerType.UnsignedInt2101010Rev:
                default:
                    throw new NotSupportedException("The specified vertex attribute format's size in bytes cannot be deciphered by the pointer type.");

            }
        }
    }
}
