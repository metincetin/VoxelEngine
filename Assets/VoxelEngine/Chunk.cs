using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using NoiseTest;

namespace VoxelEngine
{
    public class Chunk
    {
        public const byte CHUNK_WIDTH = 20;
        public const byte CHUNK_HEIGHT = 50;
        public const byte CHUNK_DEPTH = 20;

        public event Action MeshUpdated;

        private static VertexAttributeDescriptor[] _layout;

        public Mesh Mesh { get; set; }
        public Matrix4x4 WorldToLocal { get; private set; }

        private byte[] _blocks;
        private List<VertexData> _tempVD;

        // front, right, back, left
        public Chunk[] NeighbouringChunks { get; private set; } = new Chunk[4];
        public Vector2Int ChunkWorldPosition;

        private enum Neighbour
        {
            Front = 0,
            Right = 1,
            Back = 2,
            Left = 3
        }

        static Chunk()
        {
            _layout = new VertexAttributeDescriptor[]{
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UInt8, 4),
        };
        }
        public Chunk(Matrix4x4 worldToLocal)
        {
            _blocks = new byte[CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH];
            Mesh = new Mesh();
            WorldToLocal = worldToLocal;

            _tempVD = new List<VertexData>(10000);
        }

        public bool IsVoxelValid(int x, int y, int z)
        {
            return x >= 0 && x < CHUNK_WIDTH && y >= 0 && y < CHUNK_HEIGHT && z >= 0 && z < CHUNK_DEPTH;
        }

        public void SetBlock(int x, int y, int z, byte blockId)
        {
            SetBlock(PositionToIndex(x, y, z), blockId);
        }

        private int PositionToIndex(int x, int y, int z)
        {
            return z * (CHUNK_WIDTH * CHUNK_HEIGHT) + y * CHUNK_WIDTH + x;
        }

        public void ReplaceInRadius(int x, int y, int z, float radius, byte blockId)
        {
            for (int xx = 0; xx < CHUNK_WIDTH; xx++)
            {
                for (int yy = 0; yy < CHUNK_HEIGHT; yy++)
                {
                    for (int zz = 0; zz < CHUNK_DEPTH; zz++)
                    {
                        if (Vector3.Distance(new Vector3(xx, yy, zz), new Vector3(x, y, z)) < radius)
                        {
                            SetBlockInternal(PositionToIndex(xx, yy, zz), blockId);
                        }
                    }
                }
            }

            GenerateMesh();
        }

        private Vector3Int IndexToPosition(int index)
        {
            int x = index % CHUNK_WIDTH;
            int y = (index / CHUNK_WIDTH) % CHUNK_HEIGHT;
            int z = (index / (CHUNK_WIDTH * CHUNK_HEIGHT));

            return new Vector3Int(x, y, z);
        }


        public byte GetBlockID(int x, int y, int z)
        {
            if (IsVoxelValid(x, y, z))
                return _blocks[PositionToIndex(x, y, z)];

            return 0;
        }
        public void SetBlock(int posIndex, byte blockId)
        {
            if (_blocks[posIndex] == blockId) return;
            var p = IndexToPosition(posIndex);

            SetBlockInternal(posIndex, blockId);
            if (p.x <= 0)
            {
                GetNeighbour(Neighbour.Left).GenerateMesh();
            }
            else
            if (p.x >= CHUNK_WIDTH)
            {
                GetNeighbour(Neighbour.Right).GenerateMesh();
            }

            if (p.z >= CHUNK_DEPTH)
            {
                GetNeighbour(Neighbour.Front).GenerateMesh();
            }
            else if (p.z <= 0)
            {
                GetNeighbour(Neighbour.Back).GenerateMesh();
            }

            GenerateMesh();
        }

        private void SetBlockInternal(int posIndex, byte blockId)
        {
            _blocks[posIndex] = blockId;
        }

        public void FillChunk(byte blockId)
        {
            Array.Fill(_blocks, blockId);
            GenerateMesh();
        }
        public void RandomizeChunk(byte min, byte max)
        {
            for (int i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = (byte)UnityEngine.Random.Range(min, max);
            }
            GenerateMesh();
        }

        public void GenerateMesh()
        {
            if (Mesh)
                Mesh.Clear();
            else
            {
                Mesh = new Mesh();
            }



            _tempVD.Clear();
            for (int x = 0; x < CHUNK_WIDTH; x++)
            {

                for (int y = 0; y < CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < CHUNK_DEPTH; z++)
                    {
                        var blockId = GetBlockID(x, y, z);
                        if (blockId == 0) continue;
                        var topBlockId = GetBlockID(x, y + 1, z);
                        var bottomBlockId = GetBlockID(x, y - 1, z);
                        var leftBlockId = GetBlockID(x - 1, y, z);
                        var rightBlockId = GetBlockID(x + 1, y, z);
                        var frontBlockId = GetBlockID(x, y, z + 1);
                        var backBlockId = GetBlockID(x, y, z - 1);

                        byte[] neighbors = new byte[]{
                        topBlockId,bottomBlockId,0, 0
                    };

                        // top
                        if (topBlockId == 0)
                        {
                            var v0 = new VertexData(x, y + 1, z, Vector3.up, new Vector2(0, 0), 0, blockId, neighbors);
                            var v1 = new VertexData(x + 1, y + 1, z, Vector3.up, new Vector2(1, 0), 0, blockId, neighbors);
                            var v2 = new VertexData(x + 1, y + 1, z + 1, Vector3.up, new Vector2(1, 1), 0, blockId, neighbors);
                            var v3 = new VertexData(x, y + 1, z + 1, Vector3.up, new Vector2(0, 1), 0, blockId, neighbors);
                            _tempVD.Add(v0);
                            _tempVD.Add(v3);
                            _tempVD.Add(v2);

                            _tempVD.Add(v0);
                            _tempVD.Add(v2);
                            _tempVD.Add(v1);
                        }


                        // bottom
                        if (bottomBlockId == 0)
                        {
                            var v0 = new VertexData(x, y, z, Vector3.down, new Vector2(0, 0), 0, blockId, neighbors);
                            var v1 = new VertexData(x + 1, y, z, Vector3.down, new Vector2(1, 0), 0, blockId, neighbors);
                            var v2 = new VertexData(x + 1, y, z + 1, Vector3.down, new Vector2(1, 1), 0, blockId, neighbors);
                            var v3 = new VertexData(x, y, z + 1, Vector3.down, new Vector2(0, 1), 0, blockId, neighbors);
                            _tempVD.Add(v0);
                            _tempVD.Add(v2);
                            _tempVD.Add(v3);

                            _tempVD.Add(v0);
                            _tempVD.Add(v1);
                            _tempVD.Add(v2);
                        }

                        // right
                        if (rightBlockId == 0 && GetBlockFromNeighbour(Neighbour.Right, x + 1, y, z) == 0)
                        {
                            var v0 = new VertexData(x + 1, y, z, Vector3.right, new Vector2(0, 0), 1, blockId, neighbors);
                            var v1 = new VertexData(x + 1, y + 1, z, Vector3.right, new Vector2(1, 0), 1, blockId, neighbors);
                            var v2 = new VertexData(x + 1, y + 1, z + 1, Vector3.right, new Vector2(1, 1), 1, blockId, neighbors);
                            var v3 = new VertexData(x + 1, y, z + 1, Vector3.right, new Vector2(0, 1), 1, blockId, neighbors);
                            _tempVD.Add(v0);
                            _tempVD.Add(v1);
                            _tempVD.Add(v2);

                            _tempVD.Add(v0);
                            _tempVD.Add(v2);
                            _tempVD.Add(v3);
                        }

                        // left
                        if (leftBlockId == 0 && GetBlockFromNeighbour(Neighbour.Left, x - 1, y, z) == 0)
                        {
                            var v0 = new VertexData(x, y, z, Vector3.left, new Vector2(0, 0), 1, blockId, neighbors);
                            var v1 = new VertexData(x, y + 1, z, Vector3.left, new Vector2(1, 0), 1, blockId, neighbors);
                            var v2 = new VertexData(x, y + 1, z + 1, Vector3.left, new Vector2(1, 1), 1, blockId, neighbors);
                            var v3 = new VertexData(x, y, z + 1, Vector3.left, new Vector2(0, 1), 1, blockId, neighbors);
                            _tempVD.Add(v0);
                            _tempVD.Add(v2);
                            _tempVD.Add(v1);

                            _tempVD.Add(v0);
                            _tempVD.Add(v3);
                            _tempVD.Add(v2);
                        }

                        // back
                        if (backBlockId == 0 && GetBlockFromNeighbour(Neighbour.Back, x, y, z - 1) == 0)
                        {
                            var v0 = new VertexData(x, y, z, Vector3.back, new Vector2(0, 0), 1, blockId, neighbors);
                            var v1 = new VertexData(x, y + 1, z, Vector3.back, new Vector2(1, 0), 1, blockId, neighbors);
                            var v2 = new VertexData(x + 1, y + 1, z, Vector3.back, new Vector2(1, 1), 1, blockId, neighbors);
                            var v3 = new VertexData(x + 1, y, z, Vector3.back, new Vector2(0, 1), 1, blockId, neighbors);
                            _tempVD.Add(v0);
                            _tempVD.Add(v1);
                            _tempVD.Add(v2);

                            _tempVD.Add(v0);
                            _tempVD.Add(v2);
                            _tempVD.Add(v3);
                        }

                        // front
                        if (frontBlockId == 0 && GetBlockFromNeighbour(Neighbour.Front, x, y, z + 1) == 0)
                        {
                            var v0 = new VertexData(x, y, z + 1, Vector3.forward, new Vector2(0, 0), 1, blockId, neighbors);
                            var v1 = new VertexData(x, y + 1, z + 1, Vector3.forward, new Vector2(1, 0), 1, blockId, neighbors);
                            var v2 = new VertexData(x + 1, y + 1, z + 1, Vector3.forward, new Vector2(1, 1), 1, blockId, neighbors);
                            var v3 = new VertexData(x + 1, y, z + 1, Vector3.forward, new Vector2(0, 1), 1, blockId, neighbors);
                            _tempVD.Add(v0);
                            _tempVD.Add(v2);
                            _tempVD.Add(v1);

                            _tempVD.Add(v0);
                            _tempVD.Add(v3);
                            _tempVD.Add(v2);
                        }
                    }
                }
            }

            var vCount = _tempVD.Count;
            Mesh.SetVertexBufferParams(vCount, _layout);
            var verts = new NativeArray<VertexData>(vCount, Allocator.Temp);
            verts.CopyFrom(_tempVD.ToArray());

            Mesh.SetVertexBufferData(verts, 0, 0, vCount);

            var indices = new int[vCount];

            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            Mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            Mesh.RecalculateBounds();
            MeshUpdated?.Invoke();

            verts.Dispose();
        }


        private Chunk GetNeighbour(Neighbour neighbour)
        {
            int neighbourId = (int)neighbour;

            return NeighbouringChunks[neighbourId];
        }
        private byte GetBlockFromNeighbour(Neighbour neighbour, int x, int y, int z)
        {
            var ch = GetNeighbour(neighbour);
            if (ch == null) return 0;

            Vector4 blockWorld = WorldToLocal.inverse * new Vector4(x, y, z, 1);
            Vector3 chLocal = ch.WorldToLocal * blockWorld;

            return ch.GetBlockID((int)chLocal.x, (int)chLocal.y, (int)chLocal.z);
        }

        /// <summary>
        /// Fills the chunk with open simplex noise map.
        /// Does not generate the mesh, you need to manually call GenerateMesh after this.
        /// </summary>
        public void NoiseFill(OpenSimplexNoise noise)
        {
            const int SNOW_START_HEIGHT = 30;
            const int STONE_START_HEIGHT = 10;

            int octave2Seed = 41433;
            int octave3Seed = 411433;

            var localToWorld = WorldToLocal.inverse;
            for (int z = 0; z < CHUNK_DEPTH; z++)
            {
                for (int x = 0; x < CHUNK_WIDTH; x++)
                {
                    Vector3 worldPos = localToWorld * new Vector4(x, 0, z, 1);

                    float noiseVal = (float)(noise.Evaluate(worldPos.x * 0.01, (worldPos.z) * 0.01) + 1) * 0.5f;
                    float noise2Val = (float)noise.Evaluate(worldPos.x * 0.038 + octave2Seed, (worldPos.z) * 0.038 + octave2Seed);
                    float noise3Val = (float)(noise.Evaluate(worldPos.x * 0.008 + octave3Seed, (worldPos.z) * 0.008 + octave3Seed) + 1) * 0.5f;

                    noiseVal = noiseVal * 2 + noise2Val * 0.3f + noise3Val * 1.2f;
                    noiseVal /= 3.5f;

                    float height = noiseVal * (CHUNK_HEIGHT - 10) + 10;

                    for (int y = 0; y < height; y++)
                    {
                        var i = PositionToIndex(x, y, z);
                        if (y < STONE_START_HEIGHT)
                        {
                            _blocks[i] = 2;
                        }
                        else
                        // tend towards upper height, with smoothing (-0.03)
                        if (y > SNOW_START_HEIGHT && Mathf.Pow(UnityEngine.Random.value, 1 - Mathf.Min(Mathf.InverseLerp(SNOW_START_HEIGHT, CHUNK_HEIGHT - 5, y) * 2.5f, 1)) > 0.5f)
                            _blocks[i] = 3;
                        else
                            _blocks[i] = 1;
                    }
                }
            }
        }
    }
}