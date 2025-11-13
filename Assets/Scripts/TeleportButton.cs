using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab; // Required for HandGrabInteractable

/// <summary>
/// Teleports a specified object to a destination when this object is grabbed.
/// This version correctly uses the WhenStateChanged event.
/// </summary>
public class TeleportOnGrab : MonoBehaviour
{
    [Tooltip("The HandGrabInteractable that will trigger the teleport.")]
    [SerializeField]
    private HandGrabInteractable _handGrabInteractable;

    [Tooltip("The object to teleport.")]
    [SerializeField]
    private Transform _objectToTeleport;

    [Tooltip("The destination to teleport the object to.")]
    [SerializeField]
    private Transform _teleportDestination;

    void Start()
    {
        if (_handGrabInteractable == null)
        {
            _handGrabInteractable = GetComponent<HandGrabInteractable>();
        }

        if (_handGrabInteractable == null)
        {
            Debug.LogError("HandGrabInteractable is not assigned and could not be found on this GameObject!", this);
            return;
        }

        // CORRECTED: Subscribe to the WhenStateChanged C# event using '+='.
        // This is the correct event inherited from the base Interactable class.
        _handGrabInteractable.WhenStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (_handGrabInteractable != null)
        {
            // CORRECTED: Unsubscribe from the event using '-='.
            _handGrabInteractable.WhenStateChanged -= HandleStateChanged;
        }
    }

    /// <summary>
    /// This method is called whenever the state of the HandGrabInteractable changes.
    /// </summary>
    /// <param name="args">Contains information about the state change.</param>
    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        // We only want to teleport when the object is first grabbed,
        // which corresponds to the state changing to 'Select'.
        if (args.NewState == InteractableState.Select)
        {
            HandleGrab();
        }
    }

    /// <summary>
    /// Contains the actual teleport logic.
    /// </summary>
    private void HandleGrab()
    {
        Debug.Log($"'{this.name}' was grabbed! Teleporting '{_objectToTeleport.name}'.");

        if (_objectToTeleport != null && _teleportDestination != null)
        {
            _objectToTeleport.position = _teleportDestination.position;
        }
        else
        {
            Debug.LogWarning("Object to teleport or destination is not set.", this);
        }
    }
}