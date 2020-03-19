using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace TrippyGL
{
    /// <summary>
    /// An abstract class for <see cref="BufferObjectSubset"/>-s that will manage a struct type across the entire subset.
    /// </summary>
    /// <typeparam name="T">The type of struct (element) this <see cref="DataBufferSubset{T}"/> will manage.</typeparam>
    public abstract class DataBufferSubset<T> : BufferObjectSubset, IDataBufferSubset where T : unmanaged
    {
        /// <summary>The length of the subset's storage measured in elements.</summary>
        public uint StorageLength { get; private set; }

        /// <summary>The size of each element in the subset's storage measured in bytes.</summary>
        public uint ElementSize { get; }

        /// <summary>
        /// Creates a <see cref="DataBufferSubset{T}"/> with the given <see cref="BufferObject"/>
        /// and target, offset into the buffer in bytes and storage length in elements.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="bufferTarget">The <see cref="BufferTargetARB"/> this subset will always bind to.</param>
        /// <param name="storageOffsetBytes">The offset into the <see cref="BufferObject"/>'s storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        internal DataBufferSubset(BufferObject bufferObject, BufferTargetARB bufferTarget, uint storageOffsetBytes, uint storageLength)
            : base(bufferObject, bufferTarget)
        {
            ElementSize = (uint)Marshal.SizeOf<T>();
            InitializeStorage(storageOffsetBytes, storageLength * ElementSize);
            StorageLength = storageLength;
        }

        /// <summary>
        /// Creates a <see cref="DataBufferSubset{T}"/> with the given <see cref="BufferObject"/>
        /// and target, offset into the buffer in bytes and storage length in elements.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="bufferTarget">The <see cref="BufferTargetARB"/> this subset will always bind to.</param>
        /// <param name="storageOffsetBytes">The offset into the <see cref="BufferObject"/>'s storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        /// <param name="data">A <see cref="ReadOnlySpan{T}"/> containing the initial data to set to the subset.</param>
        /// <param name="dataWriteOffset">The offset into the subset's storage at which to start writting the initial data.</param>
        internal DataBufferSubset(BufferObject bufferObject, BufferTargetARB bufferTarget, uint storageOffsetBytes, uint storageLength, ReadOnlySpan<T> data, uint dataWriteOffset = 0)
            : this(bufferObject, bufferTarget, storageOffsetBytes, storageLength)
        {
            SetData(data, dataWriteOffset);
        }

        /// <summary>
        /// Creates a <see cref="DataBufferSubset{T}"/> with the given <see cref="BufferObject"/>
        /// and target, with the subset covering the entire buffer's storage.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="bufferTarget">The <see cref="BufferTargetARB"/> this subset will always bind to.</param>
        internal DataBufferSubset(BufferObject bufferObject, BufferTargetARB bufferTarget) : base(bufferObject, bufferTarget)
        {
            ElementSize = (uint)Marshal.SizeOf<T>();
            InitializeStorage(0, bufferObject.StorageLengthInBytes);
            StorageLength = StorageLengthInBytes / ElementSize;

            if (StorageLength * ElementSize != StorageLengthInBytes)
                throw new ArgumentException("The provided " + nameof(BufferObject) + "'s "
                    + nameof(BufferObject.StorageLengthInBytes) + " should be a multiple of " + nameof(ElementSize));
        }

        /// <summary>
        /// Creates a <see cref="DataBufferSubset{T}"/> with the given <see cref="BufferObject"/>
        /// and target, with the subset covering the entire buffer's storage and sets initial data.
        /// </summary>
        /// <param name="bufferObject">The <see cref="BufferObject"/> this subset will belong to.</param>
        /// <param name="bufferTarget">The <see cref="BufferTargetARB"/> this subset will always bind to.</param>
        /// <param name="data">A <see cref="ReadOnlySpan{T}"/> containing the initial data to set to the subset.</param>
        /// <param name="dataWriteOffset">The offset into the subset's storage at which to start writting the initial data.</param>
        internal DataBufferSubset(BufferObject bufferObject, BufferTargetARB bufferTarget, ReadOnlySpan<T> data, uint dataWriteOffset = 0)
            : this(bufferObject, bufferTarget)
        {
            SetData(data, dataWriteOffset);
        }

        /// <summary>
        /// Creates a <see cref="DataBufferSubset{T}"/> that occupies the same area in the same buffer as
        /// another buffer subset but has another <see cref="BufferTargetARB"/>.
        /// </summary>
        /// <param name="subsetToCopy">The <see cref="BufferObjectSubset"/> to copy the range form.</param>
        /// <param name="bufferTarget">The <see cref="BufferTargetARB"/> this subset will always bind to.</param>
        internal DataBufferSubset(BufferObjectSubset subsetToCopy, BufferTargetARB bufferTarget) : base(subsetToCopy, bufferTarget)
        {
            ElementSize = (uint)Marshal.SizeOf<T>();
            StorageLength = StorageLengthInBytes / ElementSize;

            if (StorageLength * ElementSize != StorageLengthInBytes)
                throw new ArgumentException("The provided " + nameof(BufferObjectSubset) + "'s "
                    + nameof(StorageLengthInBytes) + " should be a multiple of " + nameof(ElementSize));
        }

        /// <summary>
        /// Creates a <see cref="DataBufferSubset{T}"/> that occupies the same area in the same buffer and uses the
        /// same struct type as another <see cref="DataBufferSubset{T}"/> but has different <see cref="BufferTargetARB"/>.
        /// </summary>
        /// <param name="copy">The <see cref="DataBufferSubset{T}"/> to copy the range from.</param>
        /// <param name="bufferTarget">The <see cref="BufferTargetARB"/> this subset will always bind to.</param>
        internal DataBufferSubset(DataBufferSubset<T> copy, BufferTargetARB bufferTarget) : base(copy, bufferTarget)
        {
            ElementSize = copy.ElementSize;
            StorageLength = copy.StorageLength;
        }

        /// <summary>
        /// Sets the data of a specified part of this subset's storage.
        /// The amount of elements written is the length of the given <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <param name="data">The <see cref="ReadOnlySpan{T}"/> containing the data to set.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start writing to, measured in elements.</param>
        public unsafe void SetData(ReadOnlySpan<T> data, uint storageOffset = 0)
        {
            if (storageOffset < 0 || storageOffset >= StorageLength)
                throw new ArgumentOutOfRangeException(nameof(storageOffset), storageOffset, nameof(storageOffset) + " must be in the range [0, " + nameof(StorageLength) + ")");

            if (data.Length + storageOffset > StorageLength)
                throw new BufferCopyException("Tried to write past the subset's length");

            Buffer.GraphicsDevice.BindBuffer(this);
            fixed (void* ptr = &data[0])
                Buffer.GL.BufferSubData(BufferTarget, (int)(storageOffset * ElementSize + StorageOffsetInBytes), (uint)data.Length * ElementSize, ptr);
        }

        /// <summary>
        /// Gets the data of a specified part of this subset's storage.
        /// The amount of elements read is the length of the given <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="data">The <see cref="Span{T}"/> to which the returned data will be written to.</param>
        /// <param name="storageOffset">The offset into the subset's storage to start reading from, measured in elements.</param>
        public unsafe void GetData(Span<T> data, uint storageOffset = 0)
        {
            if (storageOffset < 0 || storageOffset >= StorageLength)
                throw new ArgumentOutOfRangeException(nameof(storageOffset), storageOffset, nameof(storageOffset) + " must be in the range [0, " + nameof(StorageLength) + ")");

            if (data.Length + storageOffset > StorageLength)
                throw new BufferCopyException("Tried to read past the subset's length");

            Buffer.GraphicsDevice.BindBuffer(this);
            fixed (void* ptr = &data[0])
                Buffer.GL.GetBufferSubData(BufferTarget, (int)(storageOffset * ElementSize + StorageOffsetInBytes), (uint)data.Length * ElementSize, ptr);
        }

        /// <summary>
        /// Changes the subset location of this <see cref="DataBufferSubset{T}"/>.
        /// </summary>
        /// <param name="storageOffsetBytes">The offset into the <see cref="BufferObject"/>'s storage where this subset begins.</param>
        /// <param name="storageLength">The length of this subset measured in elements.</param>
        public void ResizeSubset(uint storageOffsetBytes, uint storageLength)
        {
            InitializeStorage(storageOffsetBytes, storageLength * ElementSize);
            StorageLength = storageLength;
        }

        public override string ToString()
        {
            return string.Concat(base.ToString(),
                ", " + nameof(StorageLength) + "=", StorageLength.ToString(),
                ", " + nameof(ElementSize) + "=", ElementSize.ToString()
            );
        }

        /// <summary>
        /// Calculates the required storage length in bytes required for a
        /// <see cref="DataBufferSubset{T}"/> with the specified storage length.
        /// </summary>
        /// <param name="storageLength">The desired length for the <see cref="DataBufferSubset{T}"/> measured in elements.</param>
        public static uint CalculateRequiredSizeInBytes(uint storageLength)
        {
            return (uint)Marshal.SizeOf<T>() * storageLength;
        }

        /// <summary>
        /// Copies data from a source <see cref="DataBufferSubset{T}"/> to a destination <see cref="DataBufferSubset{T}"/>.
        /// </summary>
        /// <param name="source">The <see cref="DataBufferSubset{T}"/> to copy data from.</param>
        /// <param name="sourceOffset">The index of the first element to copy from the source subset.</param>
        /// <param name="dest">The <see cref="DataBufferSubset{T}"/> to write data to.</param>
        /// <param name="destOffset">The index of of the first element to write on the dest subset.</param>
        /// <param name="dataLength">The amount of elements to copy.</param>
        public static void CopyBuffers(DataBufferSubset<T> source, uint sourceOffset, DataBufferSubset<T> dest, uint destOffset, uint dataLength)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            GraphicsDevice g = source.Buffer.GraphicsDevice;
            if (g != dest.Buffer.GraphicsDevice)
                throw new InvalidOperationException("You can't copy data between buffers from different " + nameof(GraphicsDevice) + "-s");

            if (sourceOffset < 0 || sourceOffset > source.StorageLength)
                throw new ArgumentOutOfRangeException(nameof(sourceOffset), sourceOffset, nameof(sourceOffset) + " must be in the range [0, " + nameof(source.StorageLength) + ")");

            if (destOffset < 0 || destOffset > dest.StorageLength)
                throw new ArgumentOutOfRangeException(nameof(destOffset), destOffset, nameof(destOffset) + " must be in the range [0, " + nameof(dest.StorageLength) + ")");

            if (sourceOffset + dataLength > source.StorageLength)
                throw new BufferCopyException("There isn't enough data in the source buffer to copy " + nameof(dataLength) + " elements");

            if (destOffset + dataLength > dest.StorageLength)
                throw new BufferCopyException("There isn't enough data in the dest buffer to copy " + nameof(dataLength) + " elements");

            if (source == dest)
            {
                // We're copying from and to the same buffer? Let's ensure the sections don't overlap then

                if ((destOffset + dataLength <= sourceOffset) || (destOffset > sourceOffset + dataLength))
                    throw new BufferCopyException("When copying to and from the same " + nameof(BufferObject) + ", the ranges must not overlap");
                // This checks that the dest range is either fully to the left or fully to the right of the source range
            }

            // Everything looks fine, let's perform the copy operation!
            g.CopyReadBuffer = source.Buffer;
            g.CopyWriteBuffer = dest.Buffer;
            uint elementSize = source.ElementSize;
            source.Buffer.GL.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.CopyWriteBuffer, (int)(source.StorageOffsetInBytes + sourceOffset * elementSize), (int)(dest.StorageOffsetInBytes + destOffset * elementSize), dataLength * elementSize);
        }

        /// <summary>
        /// Copies all the data from a source <see cref="DataBufferSubset{T}"/> to a destination <see cref="DataBufferSubset{T}"/>.
        /// </summary>
        /// <param name="source">The <see cref="DataBufferSubset{T}"/> to copy data from.</param>
        /// <param name="dest">The <see cref="DataBufferSubset{T}"/> to write data to.</param>
        public static void CopyBuffers(DataBufferSubset<T> source, DataBufferSubset<T> dest)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            CopyBuffers(source, 0, dest, 0, source.StorageLength);
        }
    }

    /// <summary>
    /// This interface is used to be able to access properties and methods of a
    /// <see cref="DataBufferSubset{T}"/> without caring about it's type param.
    /// </summary>
    internal interface IDataBufferSubset
    {
        uint StorageLength { get; }
        uint ElementSize { get; }
    }
}
