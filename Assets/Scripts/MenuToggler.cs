using UnityEngine;

public class MenuToggler : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject canvasRoot;   // The Canvas you want to show/hide
    public Transform headCamera;    // Your 'CenterEyeAnchor'
    public CanvasLogic canvasLogic;

    [Header("Settings")]
    public float distance = 0.5f;   // How far away it spawns
    public float heightOffset = 0.0f; // Adjust if it feels too high/low

    void Start()
    {
        // Optional: Ensure it starts hidden
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }

    void Update()
    {
        // OVRInput.Button.One maps to 'A' on Right Controller (or 'X' on Left)
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) && !canvasLogic.readyScreen)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        bool isActivating = !canvasRoot.activeSelf;

        if (isActivating)
        {
            // 1. Move it to the new position before showing it
            RepositionCanvas();
            canvasRoot.SetActive(true);
        }
        else
        {
            // 2. Just hide it
            canvasRoot.SetActive(false);
        }
    }

    void RepositionCanvas()
    {
        // Calculate position: Start at head, move forward by 'distance'
        Vector3 targetPosition = headCamera.position + (headCamera.forward * distance);

        // Optional: Flatten the height? 
        // If you want it to spawn at eye level but not tilted up/down, uncomment this:
        // targetPosition.y = headCamera.position.y; 

        canvasRoot.transform.position = targetPosition + new Vector3(0, heightOffset, 0);

        // Make it face the user
        canvasRoot.transform.LookAt(headCamera);

        // FIX: LookAt makes the Z-axis point at you. 
        // UI is usually drawn on the back of the Z-axis, so we spin it 180 degrees.
        canvasRoot.transform.Rotate(0, 180, 0);
    }
}