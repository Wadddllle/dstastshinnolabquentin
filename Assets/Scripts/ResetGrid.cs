using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class ResetGrid : MonoBehaviour
{
    [Tooltip("The HandGrabInteractable that will trigger the reset.")]
    [SerializeField]
    private HandGrabInteractable _handGrabInteractable;

    [Header("Dependencies")]
    [Tooltip("Optional: Drag VisionScanner here to toggle it on/off.")]
    [SerializeField] private VisionScanner _visionScanner;

    void Start()
    {
        if (_handGrabInteractable == null)
            _handGrabInteractable = GetComponent<HandGrabInteractable>();

        if (_handGrabInteractable == null)
        {
            Debug.LogError("HandGrabInteractable missing!", this);
            return;
        }

        _handGrabInteractable.WhenStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (_handGrabInteractable != null)
            _handGrabInteractable.WhenStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        // Trigger when grabbed (Select state)
        if (args.NewState == InteractableState.Select)
        {
            ResetViewGrid();
        }
    }

    private void ResetViewGrid()
    {
        // 1. Clear the Grid Visuals
        if (TacticalGrid.Instance != null)
        {
            TacticalGrid.Instance.ClearGrid();
        }
        else
        {
            Debug.LogError("TacticalGrid Instance is null! Is the script in the scene?");
        }

        // 2. (Optional) Enable the Scanner if it was disabled
        // This solves your issue: Start the VisionScanner component as DISABLED in Inspector.
        // Only enable it after you grab this "Start/Reset" button.
        if (_visionScanner != null)
        {
            if (!_visionScanner.enabled)
            {
                _visionScanner.enabled = true;
                Debug.Log("Vision Scanner Started!");
            }
        }
    }
}