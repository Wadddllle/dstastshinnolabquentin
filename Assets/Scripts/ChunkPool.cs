using System.Collections.Concurrent;
using MarchingCubes;
using Unity.Collections;

public static class ChunkPool
{
    private static ConcurrentQueue<Chunk> _pool = new ConcurrentQueue<Chunk>();

    public static Chunk Get()
    {
        if (_pool.TryDequeue(out Chunk chunk))
        {
            return chunk;
        }
        // If pool is empty, create a new one
        return new Chunk();
    }

    public static void Return(Chunk chunk)
    {
        if (chunk != null)
        {
            // Optional: Zero out memory if debugging, 
            // but strictly not necessary as we overwrite it fully.
            _pool.Enqueue(chunk);
        }
    }

    // Call this OnApplicationQuit to clean up NativeArrays
    public static void DisposeAll()
    {
        while (_pool.TryDequeue(out Chunk chunk))
        {
            chunk.Dispose();
        }
    }
}