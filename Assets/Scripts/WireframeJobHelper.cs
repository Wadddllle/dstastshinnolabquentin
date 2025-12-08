using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Rendering;

public static class WireframeJobHelper
{
    [BurstCompile]
    struct ExplodeAndColorJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> SourceVertices;
        [ReadOnly] public NativeArray<int> SourceIndices;

        [WriteOnly] public NativeArray<Vector3> OutVertices;
        [WriteOnly] public NativeArray<Color> OutColors;
        [WriteOnly] public NativeArray<int> OutIndices;

        public void Execute(int i)
        {
            // 'i' is the target vertex index (0, 1, 2...)
            // We are creating a unique vertex for every corner of every triangle.

            // 1. Calculate which triangle and which corner we are on
            int triangleIndex = i / 3;
            int cornerIndex = i % 3;

            // 2. REVERSE LOGIC (The Magic Fix)
            // Normal Winding: 0, 1, 2
            // Reverse Winding: 2, 1, 0
            // We calculate the source index by flipping the offset.
            // If corner is 0, we take source offset 2.
            // If corner is 1, we take source offset 1.
            // If corner is 2, we take source offset 0.
            int reverseOffset = 2 - cornerIndex;

            // Calculate the actual index in the Source Array
            int sourceIndexPtr = (triangleIndex * 3) + reverseOffset;
            int originalVertexIndex = SourceIndices[sourceIndexPtr];

            // 3. Copy Position
            OutVertices[i] = SourceVertices[originalVertexIndex];

            // 4. Generate Barycentric Color (based on the NEW order)
            // 0 -> Red, 1 -> Green, 2 -> Blue
            if (cornerIndex == 0) OutColors[i] = new Color(1, 0, 0, 0);
            else if (cornerIndex == 1) OutColors[i] = new Color(0, 1, 0, 0);
            else OutColors[i] = new Color(0, 0, 1, 0);

            // 5. Set new Index (Linear)
            OutIndices[i] = i;
        }
    }

    public static void ProcessMesh(Mesh mesh)
    {
        // 1. Snapshot data
        using (var sourceVerts = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob))
        using (var sourceIndices = new NativeArray<int>(mesh.triangles, Allocator.TempJob))
        {
            int indexCount = sourceIndices.Length;

            // 2. Prepare Output Buffers
            var outVerts = new NativeArray<Vector3>(indexCount, Allocator.TempJob);
            var outColors = new NativeArray<Color>(indexCount, Allocator.TempJob);
            var outIndices = new NativeArray<int>(indexCount, Allocator.TempJob);

            // 3. Run Job
            var job = new ExplodeAndColorJob
            {
                SourceVertices = sourceVerts,
                SourceIndices = sourceIndices,
                OutVertices = outVerts,
                OutColors = outColors,
                OutIndices = outIndices
            };

            // Batch count of 64 is generally good for Quest
            JobHandle handle = job.Schedule(indexCount, 64);
            handle.Complete();

            // 4. Apply back to Mesh
            mesh.Clear();
            mesh.indexFormat = IndexFormat.UInt32; // Essential for high vertex counts

            mesh.SetVertices(outVerts);
            mesh.SetColors(outColors);
            mesh.SetIndices(outIndices, MeshTopology.Triangles, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            outVerts.Dispose();
            outColors.Dispose();
            outIndices.Dispose();
        }
    }
}