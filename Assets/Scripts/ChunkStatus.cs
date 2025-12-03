using UnityEngine;

public enum ChunkStatus
{
    // The chunk is fully loaded, visible, and awaiting a potential remesh.
    Idle,
    // The Coordinator has identified this coordinate as needed, and it's waiting for the Processor.
    QueuedForCreation,
    // The Processor is actively running the expensive BuildMeshTask for this chunk right now.
    IsBeingCreated,
    // The Coordinator has identified this chunk needs a remesh, and it's waiting for the Processor.
    QueuedForRemesh,
    // The Processor is actively remeshing this chunk.
    IsBeingRemeshed,
    // The Coordinator has marked this chunk for unloading.
    QueuedForDestruction
}

public class ChunkState
{
    public readonly Vector3Int coordinate;
    public ChunkInstanceV2 instance; // This can be null if the chunk hasn't been created yet.
    public ChunkStatus status;
    public float lastUpdateTime;

    public ChunkState(Vector3Int coord)
    {
        this.coordinate = coord;
        this.instance = null;
        this.status = ChunkStatus.QueuedForCreation; // A new state always starts by needing to be created.
        this.lastUpdateTime = 0;
    }
}