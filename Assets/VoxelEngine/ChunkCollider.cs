using UnityEngine;

namespace VoxelEngine
{
    public class ChunkCollider : MonoBehaviour
    {
        private Chunk _chunk;

        private MeshCollider _collider;

        public Chunk Chunk
        {
            get => _chunk;
            set
            {
                if (_chunk != null)
                {
                    _chunk.MeshUpdated -= OnMeshUpdated;
                }
                _chunk = value;
                _chunk.MeshUpdated += OnMeshUpdated;
                _collider.sharedMesh = _chunk.Mesh;
                UpdateMesh();
            }
        }

        private void UpdateMesh()
        {
            // TODO updating mesh collider should not be cool
            _collider.sharedMesh = Chunk.Mesh;
        }

        private void OnMeshUpdated()
        {
            UpdateMesh();
        }

        private void Awake()
        {
            _collider = gameObject.AddComponent<MeshCollider>();
        }

    }
}