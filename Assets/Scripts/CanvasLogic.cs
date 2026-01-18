using UnityEngine;
using UnityEngine.UI; // Needed for UI components
using TMPro; // Use this if you are using TextMeshPro for Dropdowns

public class CanvasLogic : MonoBehaviour
{
    public ChunkManager chunkManager;
    public MRUKAnchorManager mRUKAnchorManager;

    public TMP_Dropdown myDropdown; // Drag your Dropdown here!

    [Header("Dropdown Objects")]
    // Drag your 3 objects here in the Inspector
    public GameObject[] optionObjects;

    void Start()
    {
        // FIX 1: Run logic immediately on startup
        if (myDropdown != null)
        {
            Debug.Log($"[CanvasLogic] Startup: Force updating to index {myDropdown.value}");
            OnDropdownChange(myDropdown.value);
        }
        else
        {
            Debug.LogError("[CanvasLogic] You forgot to assign the 'myDropdown' in the Inspector!");
        }
    }
    // 1. SIMPLE BUTTON FUNCTION
    public void OnMyButtonClick()
    {
        Debug.Log("Button was pressed!");
        // Add your code here (e.g., spawn object, reset scene)
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