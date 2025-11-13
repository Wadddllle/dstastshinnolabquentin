using UnityEngine;
using System;
using Meta.XR.BuildingBlocks; // We need this new using statement

public class AnchorManager : MonoBehaviour
{
    [Tooltip("The object that contains all your virtual content (like the Chunk Manager). This will be parented to the anchor.")]
    [SerializeField] private Transform _worldContent;

    // Reference to the helper script we added in the Inspector
    private SpatialAnchorCoreBuildingBlock _anchorBuildingBlock;

    void Start()
    {
        if (_worldContent == null)
        {
            Debug.LogError("World Content transform is not assigned in the Inspector!", this);
            return;
        }

        // --- Step 1: Get the Building Block component ---
        _anchorBuildingBlock = GetComponent<SpatialAnchorCoreBuildingBlock>();
        if (_anchorBuildingBlock == null)
        {
            Debug.LogError("SpatialAnchorCoreBuildingBlock component not found on this GameObject! Please add it.", this);
            return;
        }

        // --- Step 2: Subscribe our local function to the completion event ---
        _anchorBuildingBlock.OnAnchorCreateCompleted.AddListener(OnAnchorCreated);


        // --- Step 3: Tell the Building Block to create an anchor ---
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
        {
            Debug.LogError("Could not find OVRCameraRig in the scene!", this);
            return;
        }

        Vector3 anchorPosition = rig.transform.position;
        anchorPosition.y = 0; // Lock to the floor

        Debug.Log("Requesting anchor creation from the Building Block...");
        // We pass 'null' for the prefab to let it create a default GameObject.
        _anchorBuildingBlock.InstantiateSpatialAnchor(null, anchorPosition, Quaternion.identity);
    }

    // --- Step 4: This function will be called automatically when the anchor is ready ---
    private void OnAnchorCreated(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
    {
        // This is the correct way to check for success with the Building Blocks system
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            Debug.Log("<color=green>Floor Anchor created successfully by the Building Block!</color>");

            // Parent all of your game content to the new, stable anchor.
            _worldContent.SetParent(anchor.transform, worldPositionStays: true);
        }
        else
        {
            Debug.LogError($"Failed to create anchor. Result: {result}");
        }
    }
}