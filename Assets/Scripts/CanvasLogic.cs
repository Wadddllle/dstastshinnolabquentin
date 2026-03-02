using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasLogic : MonoBehaviour
{
    [Header("Dependencies")]
    public ChunkManager chunkManager;
    public MRUKAnchorManager mRUKAnchorManager;
    public CoordinateMarker markerTool;
    public ZoneTool zoneTool;           // Ensure you added this from the previous step!
    public Transform playerHead;
    public AppManager appManager;

    [Header("UI Components")]
    public TMP_Dropdown myDropdown;
    public GameObject[] optionObjects;
    public TextMeshProUGUI instructionText;

    [Header("Cleanup Settings")]
    // Drag your InstructorPlacementTool, Obstacle, BreachPointSetting, and ZoneTool here
    public GameObject[] toolsToDisable;

    [Header("Settings")]
    public float detectionRadius = 1.0f;
    public float heightMax = 2.5f;

    [Header("Navigation")]
    public GlobalPathfinder globalPathfinder; // Drag the new script here

    // Internal State
    private bool _isCheckingPosition = false;

    void Start()
    {
        if (myDropdown != null)
        {
            OnDropdownChange(myDropdown.value);
        }

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
            instructionText.text = "Move to Breach Point...";
        }
    }

    void Update()
    {
        if (_isCheckingPosition)
        {
            CheckBreachPosition();
        }
    }

    public void OnMyButtonClick()
    {
        Debug.Log("1. Button Click Received!");

        // 1. Check Marker
        if (markerTool == null) { Debug.LogError("MarkerTool missing!"); return; }

        if (markerTool.savedCoordinate == Vector3.zero)
        {
            if (instructionText)
            {
                instructionText.text = "Error: Place Marker First!";
                instructionText.gameObject.SetActive(true);
            }
            return;
        }

        // 2. Check Zone (Using the method we added previously)
        if (zoneTool != null && !zoneTool.IsZoneConfirmed())
        {
            if (instructionText)
            {
                instructionText.text = "Error: Confirm Zone First!";
                instructionText.gameObject.SetActive(true);
            }
            return;
        }
        // 1. Disable the Dropdown so user can't select old tools
        if (myDropdown != null)
        {
            // Option A: Hide it completely (Recommended)
            //myDropdown.gameObject.SetActive(false);
            Destroy(myDropdown.gameObject);
            // Option B: Just make it unclickable (Greyed out)
            // myDropdown.interactable = false; 
        }

        // 2. Disable all the specific tool GameObjects you dragged in
        if (toolsToDisable != null)
        {
            foreach (GameObject tool in toolsToDisable)
            {
                if (tool != null) tool.SetActive(false);
            }
        }

        Debug.Log("Tools and UI disabled. Moving to Breach Point.");
        // -------------------------------
        Debug.Log("Button pressed! Generating Path...");

        // --- NEW: Activate Navigation ---
        if (globalPathfinder != null)
        {
            // 1. Enable GameObject
            globalPathfinder.gameObject.SetActive(true);

            // 2. Set Target to the Marker coordinate
            // We create a temp hidden object or just move the transform
            globalPathfinder.targetDestination.position = markerTool.savedCoordinate;

            // 3. GENERATE THE PATH (One Shot!)
            globalPathfinder.GeneratePath();
        }
        // --------------------------------

        if (instructionText != null)
        {
            instructionText.text = "Follow the Line...";
            instructionText.gameObject.SetActive(true);
        }
        _isCheckingPosition = true;
    }

    private void CheckBreachPosition()
    {
        Vector3 markerPos = markerTool.savedCoordinate;
        Vector3 headPos = playerHead.position;

        float horizontalDistance = Vector2.Distance(
            new Vector2(headPos.x, headPos.z),
            new Vector2(markerPos.x, markerPos.z)
        );

        bool isHeightCorrect = (headPos.y >= markerPos.y) && (headPos.y <= markerPos.y + heightMax);

        if (horizontalDistance <= detectionRadius && isHeightCorrect)
        {
            Debug.Log("Player reached Breach Point! Starting scenario...");
            TriggerFinishSetup();
        }
    }

    private void TriggerFinishSetup()
    {
        _isCheckingPosition = false;
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        // TRIGGER THE STATE CHANGE
        if (appManager._currentState is InstructorState instructorState)
        {
            // --- OLD: ToggleVisibility(false); ---

            // --- NEW: Enable Occlusion Mode ---
            if (chunkManager != null)
            {
                chunkManager.SetOcclusionMode(true);
            }

            // Hide the floor completely (assuming you don't want the floor occluding)
            if (mRUKAnchorManager != null)
            {
                mRUKAnchorManager.ToggleFloorVisibility(false);
            }

            instructorState.FinishSetup();
        }
        else
        {
            Debug.LogError("Error: We are not in InstructorState, cannot Finish Setup!");
        }
    }

    public void MeshToggle(bool isOn)
    {
        ToggleVisibility();
    }

    public void OnDropdownChange(int index)
    {
        // Safety check to prevent errors if this fires while we are shutting down
        if (optionObjects == null) return;

        if (index >= optionObjects.Length) return;

        for (int i = 0; i < optionObjects.Length; i++)
        {
            bool shouldBeActive = (i == index);
            if (optionObjects[i] != null)
            {
                optionObjects[i].SetActive(shouldBeActive);
            }
        }
    }

    private void ToggleVisibility()
    {
        chunkManager.ToggleChunkVisibility();
        mRUKAnchorManager.ToggleFloorVisibility();
    }
    private void ToggleVisibility(bool uhhh)
    {
        chunkManager.ToggleChunkVisibility(uhhh);
        mRUKAnchorManager.ToggleFloorVisibility(uhhh);
    }
}