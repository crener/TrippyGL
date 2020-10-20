﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using Silk.NET.OpenGL;
using System.Threading;
using TrippyGL;
using System.Numerics;

namespace TerrainMaker
{
    class ChunkManager : IDisposable
    {
        /// <summary>The X coordinate of the lowest loaded chunk in <see cref="chunks"/>.</summary>
        private int chunksGridStartX;
        /// <summary>The Y coordinate of the lowest loaded chunk in <see cref="chunks"/>.</summary>
        private int chunksGridStartY;

        /// <summary>The X index in <see cref="chunks"/> of the lowest loaded chunk.</summary>
        private int chunksOffsetX;
        /// <summary>The Y index in <see cref="chunks"/> of the lowest loaded chunk.</summary>
        private int chunksOffsetY;
        private int chunkRenderRadius;
        private TerrainChunk[,] chunks;

        private List<Point> generatingList = new List<Point>();
        private ConcurrentQueue<TerrainChunkData> loadingQueue = new ConcurrentQueue<TerrainChunkData>();

        Thread generatorThread;

        public ChunkManager(int chunkRenderRadius, int gridX, int gridY)
        {
            if (chunkRenderRadius <= 0)
                throw new ArgumentException(nameof(chunkRenderRadius));

            this.chunkRenderRadius = chunkRenderRadius;
            int chunksArraySize = chunkRenderRadius + chunkRenderRadius + 1;
            chunks = new TerrainChunk[chunksArraySize, chunksArraySize];
            chunksGridStartX = gridX - chunkRenderRadius;
            chunksGridStartY = gridY - chunkRenderRadius;
            chunksOffsetX = 0;
            chunksOffsetY = 0;

            StartLoadingUnloadedChunks();
        }

        public void ProcessChunks(GraphicsDevice graphicsDevice)
        {
            while (loadingQueue.TryDequeue(out TerrainChunkData chunkData))
            {
                Point arrCoords = GridToChunksArrayCoordinates(chunkData.GridX, chunkData.GridY);

                if (arrCoords.X < 0 || arrCoords.Y < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[LOADER] Tried to load chunk outside array! p=(" + chunkData.GridX + ", " + chunkData.GridY + ")");
                    Console.ResetColor();
                }
                else if (chunks[arrCoords.X, arrCoords.Y] != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[LOADER] Tried to load already present chunk?? p=(" + chunkData.GridX + ", " + chunkData.GridY + ")");
                    Console.ResetColor();
                }
                else
                {
                    chunks[arrCoords.X, arrCoords.Y] = new TerrainChunk(graphicsDevice, chunkData);
                    Console.WriteLine("[LOADER] Delisted loaded chunk OK p=(" + chunkData.GridX + ", " + chunkData.GridY + ")");
                }

                TerrainGenerator.ReturnArrays(chunkData);
            }
        }

        public void RenderAllTerrains(GraphicsDevice graphicsDevice)
        {
            for (int x = 0; x < chunks.GetLength(0); x++)
                for (int y = 0; y < chunks.GetLength(1); y++)
                    if (chunks[x, y] != null && !chunks[x, y].TerrainBuffer.IsEmpty)
                    {
                        graphicsDevice.VertexArray = chunks[x, y].TerrainBuffer;
                        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, chunks[x, y].TerrainBuffer.StorageLength);
                    }
        }

        public void RenderAllWaters(GraphicsDevice graphicsDevice)
        {
            for (int x = 0; x < chunks.GetLength(0); x++)
                for (int y = 0; y < chunks.GetLength(1); y++)
                    if (chunks[x, y] != null && !chunks[x, y].WaterBuffer.IsEmpty)
                    {
                        graphicsDevice.VertexArray = chunks[x, y].WaterBuffer;
                        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, chunks[x, y].WaterBuffer.StorageLength);
                    }
        }

        public void RenderAllUnderwaters(GraphicsDevice graphicsDevice)
        {
            for (int x = 0; x < chunks.GetLength(0); x++)
                for (int y = 0; y < chunks.GetLength(1); y++)
                    if (chunks[x, y] != null && !chunks[x, y].UnderwaterBuffer.IsEmpty)
                    {
                        graphicsDevice.VertexArray = chunks[x, y].UnderwaterBuffer;
                        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, chunks[x, y].UnderwaterBuffer.StorageLength);
                    }
        }

        private Point GridToChunksArrayCoordinates(int gridX, int gridY)
        {
            // We calculate the position of the chunk relative to the lowest loaded chunk.
            Point p = new Point(
                gridX - chunksGridStartX,
                gridY - chunksGridStartY
            );

            // If p is less than 0, it's definitely outside the grid (it's lower than the lowest chunk).
            // If p is greater than chunksArraySize, it's also outside the grid (too far up)
            if (p.X < 0 || p.Y < 0 || p.X >= chunks.GetLength(0) || p.Y >= chunks.GetLength(1))
                return new Point(-1);

            p.X = (p.X + chunksOffsetX) % chunks.GetLength(0);
            p.Y = (p.Y + chunksOffsetY) % chunks.GetLength(1);
            return p;
        }

        private Point ChunksArrayToGridCoordinates(int arrayX, int arrayY)
        {
            // We calculate the position of the chunk relative to the lowest loaded chunk.
            Point p = new Point(
                arrayX - chunksOffsetX,
                arrayY - chunksOffsetY
            );

            // Wrap them around
            if (p.X < 0) p.X += chunks.GetLength(0);
            if (p.Y < 0) p.Y += chunks.GetLength(1);

            // To translate it to grid coordinates, now we simply add chunksGridStart
            p.X += chunksGridStartX;
            p.Y += chunksGridStartY;

            return p;
        }

        public TerrainChunk GetChunkAt(int gridX, int gridY)
        {
            Point c = GridToChunksArrayCoordinates(gridX, gridY);
            return (c.X < 0 || c.Y < 0) ? chunks[c.X, c.Y] : null;
        }

        public void SetCenterChunk(int gridX, int gridY)
        {
            // We turn the gridX and gridY values from "grid center" to "grid lowest".
            gridX -= chunkRenderRadius;
            gridY -= chunkRenderRadius;

            // We calculate the offset between the current lowest loaded chunk and the new lowest.
            int diffX = gridX - chunksGridStartX;
            int diffY = gridY - chunksGridStartY;

            if (diffX == 0 && diffY == 0)
                return;

            Console.WriteLine("[MANAGER] Moved chunk! diff=(" + diffX + ", " + diffY + ")");

            if (diffX <= -chunks.GetLength(0) || diffX >= chunks.GetLength(0)
                || diffY <= -chunks.GetLength(1) || diffY >= chunks.GetLength(1))
            {
                chunksGridStartX = gridX;
                chunksGridStartY = gridY;
                chunksOffsetX = 0;
                chunksOffsetY = 0;

                for (int i = 0; i < chunks.GetLength(0); i++)
                    for (int c = 0; c < chunks.GetLength(1); c++)
                        if (chunks[i, c] != null)
                        {
                            chunks[i, c].Dispose();
                            chunks[i, c] = null;
                        }
            }
            else
            {
                if (diffX > 0)
                {
                    for (int dx = 0; dx < diffX; dx++)
                    {
                        int x = (chunksOffsetX + dx) % chunks.GetLength(0);
                        for (int y = 0; y < chunks.GetLength(1); y++)
                        {
                            chunks[x, y]?.Dispose();
                            chunks[x, y] = null;
                        }
                    }
                }
                else if (diffX < 0)
                {
                    for (int dx = diffX; dx < 0; dx++)
                    {
                        int x = (chunksOffsetX + dx + chunks.GetLength(0)) % chunks.GetLength(0);
                        for (int y = 0; y < chunks.GetLength(1); y++)
                        {
                            chunks[x, y]?.Dispose();
                            chunks[x, y] = null;
                        }
                    }
                }

                if (diffY > 0)
                {
                    for (int dy = 0; dy < diffY; dy++)
                    {
                        int y = (chunksOffsetY + dy) % chunks.GetLength(1);
                        for (int x = 0; x < chunks.GetLength(0); x++)
                        {
                            chunks[x, y]?.Dispose();
                            chunks[x, y] = null;
                        }
                    }
                }
                else if (diffY < 0)
                {
                    for (int dy = diffY; dy < 0; dy++)
                    {
                        int y = (chunksOffsetY + dy + chunks.GetLength(1)) % chunks.GetLength(1);
                        for (int x = 0; x < chunks.GetLength(0); x++)
                        {
                            chunks[x, y]?.Dispose();
                            chunks[x, y] = null;
                        }
                    }
                }

                chunksGridStartX += diffX;
                chunksGridStartY += diffY;
                chunksOffsetX = (chunksOffsetX + diffX + chunks.GetLength(0)) % chunks.GetLength(0);
                chunksOffsetY = (chunksOffsetY + diffY + chunks.GetLength(1)) % chunks.GetLength(1);
            }

            StartLoadingUnloadedChunks();
        }

        private void StartLoadingUnloadedChunks()
        {
            lock (generatingList)
            {
                generatingList.Clear();

                int centerChunkX = chunksGridStartX + chunkRenderRadius;
                int centerChunkY = chunksGridStartY + chunkRenderRadius;
                int radiusSquared = chunkRenderRadius * chunkRenderRadius;

                for (int x = 0; x < chunks.GetLength(0); x++)
                    for (int y = 0; y < chunks.GetLength(1); y++)
                        if (chunks[x, y] == null)
                        {
                            Point p = ChunksArrayToGridCoordinates(x, y);
                            if ((p.X - centerChunkX) * (p.X - centerChunkX) + (p.Y - centerChunkY) * (p.Y - centerChunkY) < radiusSquared)
                                generatingList.Add(p);
                        }

                generatingList.Sort((x, y) => (Math.Abs(y.X - centerChunkX) + Math.Abs(y.Y - centerChunkY)).CompareTo(Math.Abs(x.X - centerChunkX) + Math.Abs(x.Y - centerChunkY)));
                for (int i = 0; i < generatingList.Count; i++)
                    Console.WriteLine("[GENERATOR] Enlisted generating chunk p=(" + generatingList[i].X + ", " + generatingList[i].Y + ")");
            }

            if (generatorThread == null || !generatorThread.IsAlive)
            {
                generatorThread = new Thread(GeneratorThreadFunction);
                generatorThread.Start();
            }
        }

        private void GeneratorThreadFunction()
        {
            Console.WriteLine("[GENERATOR] Generator thread started :)");

            Point chunkCoords;
            while (generatingList.Count != 0)
            {
                lock (generatingList)
                {
                    chunkCoords = generatingList[generatingList.Count - 1];
                    generatingList.RemoveAt(generatingList.Count - 1);
                }

                TerrainChunkData chunkData = TerrainGenerator.Generate(chunkCoords.X, chunkCoords.Y);
                loadingQueue.Enqueue(chunkData);
            }

            Console.WriteLine("[GENERATOR] Generator thread stopped :)");
        }

        public void Dispose()
        {
            for (int x = 0; x < chunks.GetLength(0); x++)
                for (int y = 0; y < chunks.GetLength(1); y++)
                    if (chunks[x, y] != null)
                    {
                        chunks[x, y].Dispose();
                        chunks[x, y] = null;
                    }
            chunks = null;
        }
    }
}
