using UnityEngine;
using UnityEngine.UI; // Needed for UI components
using TMPro; // Use this if you are using TextMeshPro for Dropdowns

public class CanvasLogic : MonoBehaviour
{
    [Header("Dependencies")]
    public ChunkManager chunkManager;
    public MRUKAnchorManager mRUKAnchorManager;
    public CoordinateMarker markerTool; // Drag your CoordinateMarker script here!
    public Transform playerHead;        // Drag your CenterEyeAnchor here!
    public AppManager appManager;       // Drag your AppManager (that holds the States)

    [Header("UI Components")]
    public TMP_Dropdown myDropdown;
    public GameObject[] optionObjects;
    public TextMeshProUGUI instructionText; // Drag the new Text object here

    [Header("Settings")]
    public float detectionRadius = 1.0f; // 1 meter X/Z tolerance
    public float heightMax = 2.5f;       // 2.5 meters above marker tolerance

    // Internal State
    private bool _isCheckingPosition = false;

    void Start()
    {
        // Setup Dropdown Logic
        if (myDropdown != null)
        {
            OnDropdownChange(myDropdown.value);
        }

        // Hide the instruction text by default
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
            instructionText.text = "Move to Breach Point...";
        }
    }
    void Update()
    {
        // Only run logic if we clicked the button
        if (_isCheckingPosition)
        {
            CheckBreachPosition();
        }
    }

    // 1. BUTTON CLICK FUNCTION
    public void OnMyButtonClick()
    {
        Debug.Log("1. Button Click Received!"); // Put this at the very top

        if (markerTool == null)
        {
            Debug.LogError("MarkerTool is not assigned in Inspector!");
            return;
        }
        // 1. Check if we actually have a valid marker position
        if (markerTool.savedCoordinate == Vector3.zero)
        {
            Debug.LogWarning("Cannot start: No Breach Point Marker set!");
            if (instructionText)
            {
                instructionText.text = "Error: Place Marker First!";
                instructionText.gameObject.SetActive(true);
            }
            return;
        }

        Debug.Log("Button pressed! Waiting for player to reach marker...");

        // 2. Show the text
        if (instructionText != null)
        {
            instructionText.text = "Move to Breach Point...";
            instructionText.gameObject.SetActive(true);
        }

        // 3. Start monitoring position
        _isCheckingPosition = true;
    }

    // THE LOGIC YOU ASKED FOR
    private void CheckBreachPosition()
    {
        Vector3 markerPos = markerTool.savedCoordinate;
        Vector3 headPos = playerHead.position;

        // A. Check X/Z Distance (Ignore Y for now)
        // We use Vector2 to flatten the world coordinates
        float horizontalDistance = Vector2.Distance(
            new Vector2(headPos.x, headPos.z),
            new Vector2(markerPos.x, markerPos.z)
        );

        // B. Check Y Height
        // Head must be above the floor (markerPos.y) AND below 2.5m
        bool isHeightCorrect = (headPos.y >= markerPos.y) && (headPos.y <= markerPos.y + heightMax);

        // C. Combine Checks
        if (horizontalDistance <= detectionRadius && isHeightCorrect)
        {
            Debug.Log("Player reached Breach Point! Starting scenario...");
            TriggerFinishSetup();
        }
    }

    private void TriggerFinishSetup()
    {
        // Stop checking
        _isCheckingPosition = false;

        // Hide text
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        // TRIGGER THE STATE CHANGE
        // We need to access the current state. Assuming AppManager has a reference.
        if (appManager._currentState is InstructorState instructorState)
        {
            instructorState.FinishSetup();
        }
        else
        {
            Debug.LogError("Error: We are not in InstructorState, cannot Finish Setup!");
        }
    }
    // 2. TOGGLE FUNCTION (Boolean)
    public void MeshToggle(bool isOn)
    {
        if (isOn)
        {
            ToggleVisibility();
            Debug.Log("Toggle is ON");
        }
        else
        {
            ToggleVisibility();
            Debug.Log("Toggle is OFF");
        }
    }

    //public void MapToggle(bool isOn)
    //{
       
    //}

    // 3. DROPDOWN FUNCTION (Integer Index)
    public void OnDropdownChange(int index)
    {
        Debug.Log($"[CanvasLogic] Dropdown Changed! New Index: {index}");

        // Safety check
        if (index >= optionObjects.Length)
        {
            Debug.LogError($"[CanvasLogic] Error: You selected index {index}, but you only assigned {optionObjects.Length} GameObjects!");
            return;
        }

        // Loop through all objects
        for (int i = 0; i < optionObjects.Length; i++)
        {
            // Determine if this is the one we want
            bool shouldBeActive = (i == index);

            // Apply the state
            if (optionObjects[i] != null)
            {
                optionObjects[i].SetActive(shouldBeActive);
                Debug.Log($"   -> Object '{optionObjects[i].name}' is now {(shouldBeActive ? "ON" : "OFF")}");
            }
        }
        // Pro-tip: You can shorten the 8 lines above to just one line:
        // optionObjects[i].SetActive(i == index);
    }
    private void ToggleVisibility()
    {
        Debug.Log($"Toggling Chunk Visibility");

        chunkManager.ToggleChunkVisibility();
        mRUKAnchorManager.ToggleFloorVisibility();
    }
}