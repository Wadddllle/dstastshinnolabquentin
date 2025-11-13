using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;


namespace MarchingCubes
{

    //
    // Isosurface mesh builder with the marching cubes algorithm
    //
    sealed class MeshBuilder : System.IDisposable
    {
        #region Public members

        public Mesh Mesh => _mesh;

        public MeshBuilder(int x, int y, int z, int budget, ComputeShader compute)
          => Initialize((x, y, z), budget, compute);

        public MeshBuilder(Vector3Int dims, int budget, ComputeShader compute)
          => Initialize((dims.x, dims.y, dims.z), budget, compute);

        public void Dispose()
          => ReleaseAll();

        public void BuildIsosurface(ComputeBuffer voxels, float target, float scale)
          => RunCompute(voxels, target, scale);

        #endregion

        #region Private members

        (int x, int y, int z) _grids;
        int _triangleBudget;
        ComputeShader _compute;

        void Initialize((int, int, int) dims, int budget, ComputeShader compute)
        {
            _grids = dims;
            _triangleBudget = budget;
            _compute = compute;

            AllocateBuffers();
            AllocateMesh(3 * _triangleBudget);
        }

        void ReleaseAll()
        {
            ReleaseBuffers();
            ReleaseMesh();
        }

        void RunCompute(ComputeBuffer voxels, float target, float scale)
        {
            _counterBuffer.SetCounterValue(0);

            // Isosurface reconstruction
            _compute.SetInts("Dims", _grids);
            _compute.SetInt("MaxTriangle", _triangleBudget);
            _compute.SetFloat("Scale", scale);
            _compute.SetFloat("Isovalue", target);
            _compute.SetBuffer(0, "TriangleTable", _triangleTable);
            _compute.SetBuffer(0, "Voxels", voxels);
            _compute.SetBuffer(0, "VertexBuffer", _vertexBuffer);
            _compute.SetBuffer(0, "IndexBuffer", _indexBuffer);
            _compute.SetBuffer(0, "Counter", _counterBuffer);
            _compute.DispatchThreads(0, _grids);

            // Clear unused area of the buffers.
            _compute.SetBuffer(1, "VertexBuffer", _vertexBuffer);
            _compute.SetBuffer(1, "IndexBuffer", _indexBuffer);
            _compute.SetBuffer(1, "Counter", _counterBuffer);
            _compute.DispatchThreads(1, 1024, 1, 1);

            // Bounding box
            var ext = new Vector3(_grids.x, _grids.y, _grids.z) * scale;
            _mesh.bounds = new Bounds(Vector3.zero, ext);
        }

        #endregion

        #region Compute buffer objects

        ComputeBuffer _triangleTable;
        ComputeBuffer _counterBuffer;

        void AllocateBuffers()
        {
            // Marching cubes triangle table
            _triangleTable = new ComputeBuffer(256, sizeof(ulong));
            _triangleTable.SetData(PrecalculatedData.TriangleTable);

            // Buffer for triangle counting
            _counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        }

        void ReleaseBuffers()
        {
            _triangleTable.Dispose();
            _counterBuffer.Dispose();
        }

        #endregion

        #region Mesh objects

        Mesh _mesh;
        GraphicsBuffer _vertexBuffer;
        GraphicsBuffer _indexBuffer;

        void AllocateMesh(int vertexCount)
        {
            _mesh = new Mesh();

            // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            // Vertex position: float32 x 3
            var vp = new VertexAttributeDescriptor
              (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

            // Vertex normal: float32 x 3
            var vn = new VertexAttributeDescriptor
              (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

            // Vertex/index buffer formats
            _mesh.SetVertexBufferParams(vertexCount, vp, vn);
            _mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

            // Submesh initialization
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
                             MeshUpdateFlags.DontRecalculateBounds);

            // GraphicsBuffer references
            _vertexBuffer = _mesh.GetVertexBuffer(0);
            _indexBuffer = _mesh.GetIndexBuffer();
        }

        void ReleaseMesh()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            Object.Destroy(_mesh);
        }

        #endregion
        // Inside your MeshBuilder.cs class

        public class MeshData
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public int[] indices;
        }

        public async Task<MeshData> CopyMeshFromGPUAsync()
        {
            // ... The first part of the function for getting the count is the same ...
            var countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(_counterBuffer, countBuffer, 0);
            var request = AsyncGPUReadback.Request(countBuffer);
            while (!request.done) { await Task.Yield(); }
            uint triangleCountFromGPU = request.GetData<uint>()[0];
            countBuffer.Release();
            uint safeTriangleCount = System.Math.Min(triangleCountFromGPU, (uint)_triangleBudget);
            if (safeTriangleCount == 0) return new MeshData { vertices = new Vector3[0], normals = new Vector3[0], indices = new int[0] };


            // --- LOGIC RE-ORDERED FROM HERE ---

            // 1. Request the vertex data using the safe count.
            int vertexCount = (int)safeTriangleCount * 3;
            int sizeToRead = vertexCount * (sizeof(float) * 6);
            var vertexDataRequest = AsyncGPUReadback.Request(_vertexBuffer, sizeToRead, 0);

            while (!vertexDataRequest.done) { await Task.Yield(); }

            if (vertexDataRequest.hasError) return null;

            var data = vertexDataRequest.GetData<float>();

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var indices = new int[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                int dataIndex = i * 6;
                vertices[i] = new Vector3(data[dataIndex + 0], data[dataIndex + 1], data[dataIndex + 2]);
                normals[i] = new Vector3(data[dataIndex + 3], data[dataIndex + 4], data[dataIndex + 5]);
                indices[i] = i; // Create indices as we go
            }

            // Instead of applying to _mesh, just pack it up and return it.
            return new MeshData { vertices = vertices, normals = normals, indices = indices };
        }
    }

} // namespace MarchingCubes