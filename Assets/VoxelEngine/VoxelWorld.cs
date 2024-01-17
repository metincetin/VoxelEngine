using UnityEngine;
using NoiseTest;

namespace VoxelEngine
{
    public class VoxelWorld : MonoBehaviour
    {
        [SerializeField]
        private int _chunkRange;

        [SerializeField]
        private Material _voxelMaterial;

        private Chunk[] _chunks;
        private Bounds[] _chunkBounds;
        private ChunkRenderer[] _chunkRenderers;
        [SerializeField]
        private long _seed = -1;

        private Matrix4x4 _chunkPlacementMatrix;

        private void Awake()
        {
            float totalWidth = Chunk.CHUNK_WIDTH * _chunkRange;
            float totalHeight = Chunk.CHUNK_HEIGHT;
            float totalDepth = Chunk.CHUNK_DEPTH * _chunkRange;


            int chunkCount = _chunkRange * _chunkRange;
            _chunks = new Chunk[chunkCount];
            _chunkBounds = new Bounds[chunkCount];
            _chunkRenderers = new ChunkRenderer[chunkCount];


            var chunkScale = new Vector3(Chunk.CHUNK_WIDTH, 1, Chunk.CHUNK_DEPTH);
            //chunkScale.x = 1 / chunkScale.y;
            //chunkScale.z = 1 / chunkScale.z;

            _chunkPlacementMatrix = Matrix4x4.TRS(new Vector3(Chunk.CHUNK_WIDTH - totalWidth * 0.5f, 0, Chunk.CHUNK_DEPTH - totalDepth * 0.5f), Quaternion.identity, chunkScale);


            OpenSimplexNoise noise;
            if (_seed == -1)
            {
                noise = new OpenSimplexNoise();
            }
            else
            {
                noise = new OpenSimplexNoise(_seed);
            }
            int chunkI = 0;
            for (int y = 0; y < _chunkRange; y++)
            {
                for (int x = 0; x < _chunkRange; x++)
                {
                    var chunkRendererGO = new GameObject("ChunkRenderer", new[] { typeof(ChunkRenderer), typeof(ChunkCollider) });
                    chunkRendererGO.transform.position = new Vector3(
                        x * Chunk.CHUNK_WIDTH - totalWidth * 0.5f,
                        -totalHeight,
                        y * Chunk.CHUNK_DEPTH - totalDepth * 0.5f
                    );
                    chunkRendererGO.transform.SetParent(transform, true);

                    var chunkRenderer = chunkRendererGO.GetComponent<ChunkRenderer>();
                    chunkRenderer.ChunkMaterial = _voxelMaterial;
                    chunkRenderer.CreateChunk();
                    chunkRenderer.Chunk.NoiseFill(noise);
                    chunkRenderer.Chunk.ChunkWorldPosition = new Vector2Int(x, y);
                    //chunkRenderer.Fill();

                    var chunkCollider = chunkRendererGO.GetComponent<ChunkCollider>();

                    chunkCollider.Chunk = chunkRenderer.Chunk;

                    _chunkRenderers[chunkI] = chunkRenderer;
                    _chunks[chunkI] = chunkRenderer.Chunk;

                    chunkI++;
                }
            }

            // populating chunk neighbours
            foreach (var c in _chunks)
            {
                var cp = c.ChunkWorldPosition;
                if (cp.y + 1 < _chunkRange)
                    c.NeighbouringChunks[0] = GetChunkOfPosition(cp.x, cp.y + 1);
                if (cp.x + 1 < _chunkRange)
                    c.NeighbouringChunks[1] = GetChunkOfPosition(cp.x + 1, cp.y);
                if (cp.y > 0)
                    c.NeighbouringChunks[2] = GetChunkOfPosition(cp.x, cp.y - 1);
                if (cp.x > 0)
                    c.NeighbouringChunks[3] = GetChunkOfPosition(cp.x - 1, cp.y);
            }

            // generating the meshes
            for (int i = 0; i < _chunks.Length; i++)
            {
                Chunk c = _chunks[i];
                c.GenerateMesh();
                _chunkBounds[i] = _chunkRenderers[i].WorldBounds;
            }
        }
        public Chunk GetChunkOfPosition(int x, int y)
        {
            return _chunks[y * _chunkRange + x];
        }
        float Fract(float value)
        {
            return value - Mathf.Floor(value);
        }
        bool AlmostEquals(float a, float b)
        {
            return Mathf.Abs(a - b) < Mathf.Epsilon;
        }

        public void ReplaceInRadius(Vector3 fromPosition, float radius, byte blockId)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                var chunk = _chunks[i];
                var bounds = _chunkBounds[i];
                var mtx = chunk.WorldToLocal;

                if (bounds.Intersects(new Bounds(fromPosition, Vector3.one * radius * 2)))
                {
                    var p = mtx * new Vector4(fromPosition.x, fromPosition.y, fromPosition.z, 1);
                    chunk.ReplaceInRadius((int)p.x, (int)p.y, (int)p.z, radius, blockId);
                }
            }
        }

        public bool Raycast(Ray ray, float distance, out VoxelRaycastHit hit)
        {

            var invChunkPlacementMatrix = _chunkPlacementMatrix.inverse;

            // convert world space position to our chunk placement space.
            var chunkPosF = invChunkPlacementMatrix * new Vector4(ray.origin.x, ray.origin.y, ray.origin.z, 1);

            var chunkPos = new Vector2Int(Mathf.CeilToInt(chunkPosF.x), Mathf.CeilToInt(chunkPosF.z));


            // taken from https://github.com/StanislavPetrovV/Minecraft/blob/main/voxel_handler.py
            bool DoRaycast(Chunk chunk, out VoxelRaycastHit hit)
            {
                var mtx = chunk.WorldToLocal;

                float x1 = ray.origin.x;
                float y1 = ray.origin.y;
                float z1 = ray.origin.z;

                var p2 = ray.origin + ray.direction * distance;
                float x2 = p2.x;
                float y2 = p2.y;
                float z2 = p2.z;

                var vp = mtx * new Vector4(x1, y1, z1, 1);
                var current_voxel_pos = new Vector3Int((int)vp.x, (int)vp.y, (int)vp.z);
                var voxel_normal = Vector3.zero;
                int step_dir = -1;

                int dx = (int)Mathf.Sign(x2 - x1);
                if (AlmostEquals(x2 - y1, 0)) dx = 0;
                float delta_x;
                if (dx != 0)
                    delta_x = Mathf.Min(dx / (x2 - x1), 10000000.0f);
                else
                    delta_x = 10000000.0f;

                float max_x;
                if (dx > 0)
                {
                    max_x = delta_x * (1.0f - Fract(x1));
                }
                else
                    max_x = delta_x * Fract(x1);

                int dy = (int)Mathf.Sign(y2 - y1);
                if (AlmostEquals(y2 - y1, 0)) dy = 0;
                float delta_y = 0;
                if (dy != 0)
                    delta_y = Mathf.Min(dy / (y2 - y1), 10000000.0f);
                else
                    delta_y = 10000000.0f;

                float max_y = 0;
                if (dy > 0)
                    max_y = delta_y * (1.0f - Fract(y1));
                else max_y = delta_y * Fract(y1);

                int dz = (int)Mathf.Sign(z2 - z1);
                if (AlmostEquals(z2 - z1, 0)) dz = 0;
                float delta_z = 0;
                if (dz != 0)
                    delta_z = Mathf.Min(dz / (z2 - z1), 10000000.0f);
                else
                    delta_z = 10000000.0f;

                float max_z = 0;
                if (dz > 0)
                    max_z = delta_z * (1.0f - Fract(z1));
                else
                    max_z = delta_z * Fract(z1);

                while (!(max_x > 1.0f && max_y > 1.0f && max_z > 1.0f))
                {
                    //var result = self.get_voxel_id(voxel_world_pos = current_voxel_pos)
                    //var tmp_current_voxel_pos = new Vector3Int(Mathf.RoundToInt(vp.x), Mathf.RoundToInt(vp.y), Mathf.RoundToInt(vp.z));
                    var bid = chunk.GetBlockID(current_voxel_pos.x, current_voxel_pos.y, current_voxel_pos.z);

                    if (bid != 0)
                    {

                        if (step_dir == 0)
                            voxel_normal.x = -dx;
                        else if (step_dir == 1)
                            voxel_normal.y = -dy;
                        else
                            voxel_normal.z = -dz;
                        hit = new VoxelRaycastHit
                        {
                            VoxelNormal = voxel_normal,
                            VoxelPosition = current_voxel_pos,
                            Chunk = chunk
                        };
                        return true;
                    }

                    if (max_x < max_y)
                    {
                        if (max_x < max_z)
                        {
                            current_voxel_pos.x += dx;
                            max_x += delta_x;
                            step_dir = 0;
                        }
                        else
                        {
                            current_voxel_pos.z += dz;
                            max_z += delta_z;
                            step_dir = 2;
                        }
                    }
                    else
                    {
                        if (max_y < max_z)
                        {
                            current_voxel_pos.y += dy;
                            max_y += delta_y;
                            step_dir = 1;
                        }
                        else
                        {
                            current_voxel_pos.z += dz;
                            max_z += delta_z;
                            step_dir = 2;
                        }
                    }
                }
                hit = new VoxelRaycastHit();
                return false;
            }


            {
                if (DoRaycast(GetChunkOfPosition(chunkPos.x, chunkPos.y), out var tempHit))
                {
                    hit = tempHit;
                    return true;
                }
            }
            {
                if (chunkPos.x + 1 < _chunkRange && DoRaycast(GetChunkOfPosition(chunkPos.x + 1, chunkPos.y), out var tempHit))
                {
                    hit = tempHit;
                    return true;
                }
            }
            {
                if (chunkPos.x - 1 >= 0 && DoRaycast(GetChunkOfPosition(chunkPos.x - 1, chunkPos.y), out var tempHit))
                {
                    hit = tempHit;
                    return true;
                }
            }

            {
                if (chunkPos.y - 1 >= 0 && DoRaycast(GetChunkOfPosition(chunkPos.x, chunkPos.y - 1), out var tempHit))
                {
                    hit = tempHit;
                    return true;
                }
            }

            {
                if (chunkPos.y + 1 < _chunkRange && DoRaycast(GetChunkOfPosition(chunkPos.x, chunkPos.y + 1), out var tempHit))
                {
                    hit = tempHit;
                    return true;
                }
            }

            hit = new VoxelRaycastHit();
            return false;
        }
    }

    public struct VoxelRaycastHit
    {
        public Vector3Int VoxelPosition;
        public Vector3 VoxelNormal;
        public byte BlockId;
        public Chunk Chunk;
    }
}