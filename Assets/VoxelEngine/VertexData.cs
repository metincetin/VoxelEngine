using UnityEngine;

namespace VoxelEngine
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct VertexData
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 TexCoords;
        public byte NeighborTop;
        public byte NeighborBottom;


        public VertexData(float x, float y, float z, Vector3 normal, Vector4 texcoord, byte faceId, byte blockId, byte[] neighbors)
        {
            Position = new Vector3(x, y, z);
            Normal = normal;
            TexCoords = texcoord;
            TexCoords.z = faceId;
            TexCoords.w = blockId;
            NeighborTop = neighbors[0];
            NeighborBottom = neighbors[1];
        }
    }
}