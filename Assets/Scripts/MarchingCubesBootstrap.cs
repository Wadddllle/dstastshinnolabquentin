using UnityEngine;
using MarchingCubes;

// Add this to your ChunkManager GameObject or a separate "System" GameObject
public class MarchingCubesBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Initialize tables ONCE at startup
        Mesher.InitializeTables();
    }

    private void OnDestroy()
    {
        // Dispose tables ONCE at application exit
        Mesher.DisposeTables();
    }
}