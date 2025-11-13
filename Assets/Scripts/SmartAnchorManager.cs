//using UnityEngine;
//using System;
//using System.Linq;
//using System.Collections;
//using System.Collections.Generic;

//public class SmartAnchorManager : MonoBehaviour
//{
//    [Tooltip("The object that contains all your virtual content (like the Chunk Manager).")]
//    [SerializeField] private Transform _worldContent;

//    // A public, static reference for your AAR system to use.
//    public static Transform AnchorParent { get; private set; }

//    private bool _anchorEstablished = false;

//    void Start()
//    {
//        if (_worldContent == null)
//        {
//            Debug.LogError("World Content transform is not assigned in the Inspector!", this);
//            return;
//        }

//        // --- Priority A: Subscribe to the Scene Model Loaded event ---
//        OVRSceneManager.SceneModelLoadedSuccessfully += TryFindSceneApiFloor;

//        // --- Start the fallback timer ---
//        StartCoroutine(FallbackAnchorRoutine());
//    }

//    private void OnDestroy()
//    {
//        // Always clean up event subscriptions to prevent errors
//        OVRSceneManager.SceneModelLoadedSuccessfully -= TryFindSceneApiFloor;
//    }

//    // --- PRIORITY A: The Gold Standard (Uses Pre-Scanned Room Setup) ---
//    private void TryFindSceneApiFloor()
//    {
//        if (_anchorEstablished) return;

//        Debug.Log("Scene Model loaded. Searching for pre-scanned 'FLOOR' plane...");
//        OVRScenePlane floorPlane = OVRSceneManager.Instance.ScenePlanes.FirstOrDefault(plane => plane.HasLabel(OVRSceneManager.Classification.Floor));

//        if (floorPlane != null)
//        {
//            Debug.Log("<color=green>SUCCESS: Found pre-scanned 'FLOOR' Scene Anchor. This is the most stable anchor.</color>");
//            EstablishAnchor(floorPlane.transform);
//        }
//        else
//        {
//            Debug.LogWarning("Pre-scanned 'FLOOR' not found in Scene Model.");
//        }
//    }

//    // --- PRIORITY B & C: The "Use Anywhere" Fallback ---
//    private IEnumerator FallbackAnchorRoutine()
//    {
//        // Give the Scene API a moment to load the pre-scanned data first.
//        yield return new WaitForSeconds(2.5f);
//        if (_anchorEstablished) yield break;

//        // --- PRIORITY B: Try to find a real-time plane ---
//        Debug.LogWarning("Could not find pre-scanned floor. Attempting real-time plane detection...");

//        // Find all currently detected horizontal planes by checking their normal vector.
//        var horizontalPlanes = OVRSceneManager.Instance.ScenePlanes.Where(p =>
//            p.transform.up.y > 0.95f).ToList();

//        if (horizontalPlanes.Any())
//        {
//            // Find the lowest of the horizontal planes. This is our best guess for the floor.
//            OVRScenePlane lowestPlane = horizontalPlanes.OrderBy(p => p.transform.position.y).First();
//            Debug.Log("<color=yellow>SUCCESS: Found a real-time horizontal plane to use as floor anchor.</color>");
//            EstablishAnchor(lowestPlane.transform);
//            yield break;
//        }

//        // --- PRIORITY C: The Last Resort "Dumb" Anchor ---
//        Debug.LogError("CRITICAL: Could not find any floor planes. Falling back to an estimated floor anchor. The world may drift.");
//        var rig = FindFirstObjectByType<OVRCameraRig>();
//        GameObject dumbAnchorObject = new GameObject("Dumb_Fallback_Anchor");
//        Vector3 anchorPosition = rig.transform.position;
//        anchorPosition.y = 0; // Guess the floor is at y=0 relative to the headset's starting position
//        dumbAnchorObject.transform.position = anchorPosition;
//        dumbAnchorObject.transform.rotation = Quaternion.identity;
//        EstablishAnchor(dumbAnchorObject.transform);
//    }

//    private void EstablishAnchor(Transform anchorTransform)
//    {
//        if (_anchorEstablished) return;
//        _anchorEstablished = true;

//        AnchorParent = anchorTransform;
//        _worldContent.SetParent(AnchorParent, worldPositionStays: true);

//        Debug.Log($"<color=cyan>ANCHOR ESTABLISHED. All virtual content is now locked to '{anchorTransform.name}'.</color>");
//    }
//}