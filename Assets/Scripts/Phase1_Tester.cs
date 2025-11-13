//using UnityEngine;
//using Unity.Collections;
//using System.Threading.Tasks;
//using Anaglyph.XRTemplate; // Namespace from Lasertag's EnvironmentMapper

//// This script requires a VoxelProvider to be on the same GameObject.
//[RequireComponent(typeof(VoxelProvider))]
//public class Phase1_Tester : MonoBehaviour
//{
//    // --- CHANGE IS HERE ---
//    [Header("Dependencies")]
//    [SerializeField] private EnvironmentMapper _environmentMapper;
//    // --- END CHANGE ---
//    private VoxelProvider _voxelProvider;
//    private bool isTestRunning = false;

//    // We'll define our test chunk properties here
//    private readonly Vector3Int chunkDimensions = new Vector3Int(32, 128, 32);

//    void Start()
//    {
//        _voxelProvider = GetComponent<VoxelProvider>();
//        if (_environmentMapper == null)
//        {
//            Debug.LogError("FATAL ERROR: Environment Mapper has not been assigned in the Inspector!", this);
//            this.enabled = false; // Disable the script to prevent further errors.
//            return;
//        }
//        // --- END CHANGE ---
//        Debug.Log("Phase 1 Tester is ready. Press 'T' to run the voxel data retrieval test.");
//    }

//    void Update()
//    {
//        //// On a key press, and only if a test isn't already running...
//        //if (Input.GetKeyDown(KeyCode.T) && !isTestRunning)
//        //{
//        //    RunTest();
//        //}
//    }
//    [ContextMenu("Run test")]
//    private async void RunTest()
//    {
//        isTestRunning = true;
//        Debug.Log("<color=yellow>--- STARTING PHASE 1 TEST ---</color>");

//        // --- Calculate the chunk origin ---
//        // Get the global volume's dimensions
//        var globalVolume = _environmentMapper.volume;
//        // Add another safety check here, just in case
//        if (globalVolume == null)
//        {
//            Debug.LogError("Test FAILED: The RenderTexture 'volume' on the assigned Environment Mapper is null!", this);
//            isTestRunning = false;
//            return;
//        }

//        var globalVolumeDims = new Vector3Int(globalVolume.width, globalVolume.height, globalVolume.volumeDepth);

//        // Calculate the origin for a chunk centered in the global volume
//        // (Just like we did in the old ChunkTestRunner)
//        Vector3Int chunkVoxelOrigin = new Vector3Int(
//            (globalVolumeDims.x / 2) - (chunkDimensions.x / 2),
//            0, // Start from the floor
//            (globalVolumeDims.z / 2) - (chunkDimensions.z / 2)
//        );
//        Debug.Log($"Requesting data for chunk at origin: {chunkVoxelOrigin} with dimensions: {chunkDimensions}");

//        // --- Call the provider and await the result ---
//        NativeArray<sbyte> voxelData = await _voxelProvider.GetVoxelDataForChunk(chunkVoxelOrigin, chunkDimensions);

//        // --- Analyze the result ---
//        if (!voxelData.IsCreated || voxelData.Length == 0)
//        {
//            Debug.LogError("Test FAILED: The returned NativeArray was not created or is empty.");
//        }
//        else
//        {
//            Debug.Log($"Test SUCCESS: Received a NativeArray with {voxelData.Length} elements.");

//            // Let's analyze the contents of the array
//            int positiveCount = 0;
//            int negativeCount = 0;
//            int zeroCount = 0;
//            sbyte minValue = sbyte.MaxValue;
//            sbyte maxValue = sbyte.MinValue;

//            for (int i = 0; i < voxelData.Length; i++)
//            {
//                sbyte val = voxelData[i];
//                if (val > maxValue) maxValue = val;
//                if (val < minValue) minValue = val;

//                if (val > 0) positiveCount++;
//                else if (val < 0) negativeCount++;
//                else zeroCount++;
//            }

//            Debug.Log($"Data Analysis -> Min Value: {minValue}, Max Value: {maxValue}");
//            Debug.Log($"Data Analysis -> Positive Values: {positiveCount}, Negative Values: {negativeCount}, Zeroes: {zeroCount}");

//            // CRITICAL: We MUST dispose of the NativeArray to prevent memory leaks.
//            voxelData.Dispose();
//        }

//        Debug.Log("<color=yellow>--- TEST COMPLETE ---</color>");
//        isTestRunning = false;
//    }
//}