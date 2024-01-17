using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
    public class ChunkRenderer : MonoBehaviour
    {
        private Chunk _chunk;
        public Chunk Chunk => _chunk;

        public Material ChunkMaterial
        {
            get => _renderer.material;
            set => _renderer.material = value;
        }
        public Bounds WorldBounds => _renderer.bounds;

        private MeshRenderer _renderer;
        private MeshFilter _filter;

        private void Awake()
        {
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _filter = gameObject.AddComponent<MeshFilter>();
        }

        public void CreateChunk()
        {
            _chunk = new Chunk(transform.worldToLocalMatrix);
            _filter.mesh = _chunk.Mesh;
        }

        internal void Fill()
        {
            _chunk.FillChunk(1);
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
            Handles.Label(transform.position + new Vector3(Chunk.CHUNK_WIDTH * 0.5f, Chunk.CHUNK_HEIGHT * 0.5f, Chunk.CHUNK_DEPTH * 0.5f), Chunk.ChunkWorldPosition.ToString());
        }
#endif
    }
}