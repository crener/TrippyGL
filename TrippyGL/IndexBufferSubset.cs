using OpenTK.Graphics.OpenGL4;
using System;

namespace TrippyGL
{
    /// <summary>
    /// A <see cref="BufferObjectSubset"/> whose purpose is to store index data.
    /// </summary>
    public class IndexBufferSubset : BufferObjectSubset
    {
        private const int SizeOfUint = sizeof(uint);
        private const int SizeOfUshort = sizeof(ushort);
        private const int SizeOfByte = sizeof(byte);

        /// <summary>The length of the buffer subset's storage measured in elements.</summary>
        public int StorageLength { get; private set; }

        /// <summary>The size of each element in the buffer subset's storage measured in bytes.</summary>
        public readonly int ElementSize;

        /// <summary>Gets the amount of bytes each element occupies.</summary>
        public readonly DrawElementsType ElementType;

        /// <summary>
        /// Creates a new <see cref="IndexBufferSubset"/> with the given <see cref="BufferObject"/>
        /// and specified offset into the buffer, storage length and element type.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="storageOffsetBytes">The offset into the buffer's storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        /// <param name="elementType">The type of elements this index buffer will use.</param>
        public IndexBufferSubset(BufferObject bufferObject, int storageOffsetBytes, int storageLength, DrawElementsType elementType)
            : base(bufferObject, BufferTarget.ElementArrayBuffer)
        {
            ElementType = elementType;
            ElementSize = TrippyUtils.GetSizeInBytesOfElementType(elementType);
            ResizeSubset(storageOffsetBytes, storageLength);
        }

        /// <summary>
        /// Creates an <see cref="IndexBufferSubset"/> with the given <see cref="BufferObject"/>,
        /// with the subset covering the entire buffer object's storage.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="elementType">The type of elements this index buffer will use.</param>
        public IndexBufferSubset(BufferObject bufferObject, DrawElementsType elementType)
            : this(bufferObject, 0, bufferObject.StorageLengthInBytes, elementType)
        {

        }

        /// <summary>
        /// Creates a new IndexBufferSubset with the specified offset into the buffer, storage length, UnsignedInt element type and initial data.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="storageOffsetBytes">The offset into the buffer's storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        /// <param name="data">A <see cref="Span{T}"/> containing the initial data to set to the subset.</param>
        /// <param name="dataWriteOffset">The offset into the subset's storage at which to start writting the initial data.</param>
        public IndexBufferSubset(BufferObject bufferObject, int storageOffsetBytes, int storageLength, Span<uint> data, int dataWriteOffset = 0)
            : this(bufferObject, storageOffsetBytes, storageLength, DrawElementsType.UnsignedInt)
        {
            SetData(data, dataWriteOffset);
        }

        /// <summary>
        /// Creates a new IndexBufferSubset with the specified offset into the buffer, storage length, UnsignedShort element type and initial data.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="storageOffsetBytes">The offset into the buffer's storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        /// <param name="data">A <see cref="Span{T}"/> containing the initial data to set to the subset.</param>
        /// <param name="dataWriteOffset">The offset into the subset's storage at which to start writting the initial data.</param>
        public IndexBufferSubset(BufferObject bufferObject, int storageOffsetBytes, int storageLength, Span<ushort> data, int dataWriteOffset = 0)
            : this(bufferObject, storageOffsetBytes, storageLength, DrawElementsType.UnsignedShort)
        {
            SetData(data, dataWriteOffset);
        }

        /// <summary>
        /// Creates a new <see cref="IndexBufferSubset"/> with the specified offset into the buffer,
        /// storage length, <see cref="byte"/> element type and initial data.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="storageOffsetBytes">The offset into the buffer's storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        /// <param name="data">A <see cref="Span{T}"/> containing the initial data to set to the subset.</param>
        /// <param name="dataWriteOffset">The offset into the subset's storage at which to start writting the initial data.</param>
        public IndexBufferSubset(BufferObject bufferObject, int storageOffsetBytes, int storageLength, Span<byte> data, int dataWriteOffset = 0)
            : this(bufferObject, storageOffsetBytes, storageLength, DrawElementsType.UnsignedByte)
        {
            SetData(data, dataWriteOffset);
        }

        /// <summary>
        /// Sets the data of a specified part of this subset's storage.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> containing the data to set.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start writing to.</param>
        public void SetData(Span<uint> data, int storageOffset = 0)
        {
            Buffer.ValidateWriteOperation();
            ValidateCorrectElementType(DrawElementsType.UnsignedInt);
            ValidateSetParams(data.Length, storageOffset);

            Buffer.GraphicsDevice.BindBufferObject(Buffer);
            GL.BufferSubData(GraphicsDevice.DefaultBufferTarget, (IntPtr)(storageOffset * SizeOfUint + StorageOffsetInBytes), data.Length * SizeOfUint, ref data[0]);
        }

        /// <summary>
        /// Sets the data of a specified part of this subset's storage.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> containing the data to set.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start writing to.</param>
        public void SetData(Span<ushort> data, int storageOffset = 0)
        {
            Buffer.ValidateWriteOperation();
            ValidateCorrectElementType(DrawElementsType.UnsignedShort);
            ValidateSetParams(data.Length, storageOffset);

            Buffer.GraphicsDevice.BindBufferObject(Buffer);
            GL.BufferSubData(GraphicsDevice.DefaultBufferTarget, (IntPtr)(storageOffset * SizeOfUshort + StorageOffsetInBytes), data.Length * SizeOfUshort, ref data[0]);
        }

        /// <summary>
        /// Sets the data of a specified part of this subset's storage.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> containing the data to set.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start writing to.</param>
        public void SetData(Span<byte> data, int storageOffset = 0)
        {
            Buffer.ValidateWriteOperation();
            ValidateCorrectElementType(DrawElementsType.UnsignedByte);
            ValidateSetParams(data.Length, storageOffset);

            Buffer.GraphicsDevice.BindBufferObject(Buffer);
            GL.BufferSubData(GraphicsDevice.DefaultBufferTarget, (IntPtr)(storageOffset * SizeOfByte + StorageOffsetInBytes), data.Length * SizeOfByte, ref data[0]);
        }

        /// <summary>
        /// Gets the data of a specified part of this subset's storage.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> to which the returned data will be written to.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start reading from.</param>
        public void GetData(Span<uint> data, int storageOffset = 0)
        {
            Buffer.ValidateReadOperation();
            ValidateCorrectElementType(DrawElementsType.UnsignedInt);
            ValidateGetParams(data.Length, storageOffset);

            Buffer.GraphicsDevice.BindBufferObject(Buffer);
            GL.GetBufferSubData(GraphicsDevice.DefaultBufferTarget, (IntPtr)(storageOffset * SizeOfUint + StorageOffsetInBytes), data.Length * SizeOfUint, ref data[0]);
        }

        /// <summary>
        /// Gets the data of a specified part of this subset's storage.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> to which the returned data will be written to.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start reading from.</param>
        public void GetData(Span<ushort> data, int storageOffset = 0)
        {
            Buffer.ValidateReadOperation();
            ValidateCorrectElementType(DrawElementsType.UnsignedShort);
            ValidateGetParams(data.Length, storageOffset);

            Buffer.GraphicsDevice.BindBufferObject(Buffer);
            GL.GetBufferSubData(GraphicsDevice.DefaultBufferTarget, (IntPtr)(storageOffset * SizeOfUshort + StorageOffsetInBytes), data.Length * SizeOfUshort, ref data[0]);
        }

        /// <summary>
        /// Gets the data of a specified part of this subset's storage.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> to which the returned data will be written to.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start reading from.</param>
        public void GetData(Span<byte> data, int storageOffset = 0)
        {
            Buffer.ValidateReadOperation();
            ValidateCorrectElementType(DrawElementsType.UnsignedByte);
            ValidateGetParams(data.Length, storageOffset);

            Buffer.GraphicsDevice.BindBufferObject(Buffer);
            GL.GetBufferSubData(GraphicsDevice.DefaultBufferTarget, (IntPtr)(storageOffset * SizeOfByte + StorageOffsetInBytes), data.Length * SizeOfByte, ref data[0]);
        }

        /// <summary>
        /// Changes the subset location of this <see cref="IndexBufferSubset"/>.
        /// </summary>
        /// <param name="storageOffsetBytes">The offset into the buffer object's storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        public void ResizeSubset(int storageOffsetBytes, int storageLength)
        {
            if (storageOffsetBytes % ElementSize != 0)
                throw new ArgumentException(nameof(storageOffsetBytes) + " should be a multiple of " + nameof(ElementSize), nameof(storageOffsetBytes));

            InitializeStorage(storageOffsetBytes, storageLength * ElementSize);
            StorageLength = storageLength;
        }

        /// <summary>
        /// Checks that this index buffer's <see cref="ElementType"/> is the specified one and throws an exception if it's not.
        /// </summary>
        /// <param name="elementType">The element type to compare.</param>
        private void ValidateCorrectElementType(DrawElementsType elementType)
        {
            if (elementType != ElementType)
                throw new InvalidOperationException("To perform this operation the " + nameof(IndexBufferSubset) + "'s " + nameof(ElementType) + " must be " + elementType.ToString());
        }

        /// <summary>
        /// Validates the parameters for a set operation.
        /// </summary>
        private void ValidateSetParams(int dataLength, int storageOffset)
        {
            if (storageOffset < 0 || storageOffset >= StorageLength)
                throw new ArgumentOutOfRangeException(nameof(storageOffset), storageOffset, nameof(storageOffset) + " must be in the range [0, " + nameof(StorageLength) + ")");

            if (dataLength + storageOffset > StorageLength)
                throw new ArgumentOutOfRangeException("Tried to write past the subset's length");
        }

        /// <summary>
        /// Validates the parameters for a get operation.
        /// </summary>
        private void ValidateGetParams(int dataLength, int storageOffset)
        {
            if (storageOffset < 0 || storageOffset >= StorageLength)
                throw new ArgumentOutOfRangeException(nameof(storageOffset), storageOffset, nameof(storageOffset) + " must be in the range [0, " + nameof(StorageLength) + ")");

            if (dataLength + storageOffset > StorageLength)
                throw new ArgumentOutOfRangeException("Tried to read past the subset's length");
        }

        /// <summary>
        /// Calculates the required storage length in bytes required for a UniformBufferSubset with the specified storage length.
        /// </summary>
        /// <param name="elementType">The desired element type for the index buffer.</param>
        /// <param name="storageLength">The desired length for the subset measured in elements.</param>
        public static int CalculateRequiredSizeInBytes(DrawElementsType elementType, int storageLength)
        {
            return TrippyUtils.GetSizeInBytesOfElementType(elementType) * storageLength;
        }
    }
}