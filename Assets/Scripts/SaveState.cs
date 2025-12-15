//using UnityEngine;
//using Oculus.Interaction;
//using Oculus.Interaction.HandGrab;

//public class SaveState : MonoBehaviour
//{
//    [Tooltip("The HandGrabInteractable that will trigger the reset.")]
//    [SerializeField]
//    private HandGrabInteractable _handGrabInteractable;

//    [Header("Dependencies")]
//    [Tooltip("Optional: Drag VisionScanner here to toggle it on/off.")]
//    [SerializeField] private VisionScanner _visionScanner;

//    void Start()
//    {
//        if (_handGrabInteractable == null)
//            _handGrabInteractable = GetComponent<HandGrabInteractable>();

//        if (_handGrabInteractable == null)
//        {
//            Debug.LogError("HandGrabInteractable missing!", this);
//            return;
//        }

//        _handGrabInteractable.WhenStateChanged += HandleStateChanged;
//    }

//    private void OnDestroy()
//    {
//        if (_handGrabInteractable != null)
//            _handGrabInteractable.WhenStateChanged -= HandleStateChanged;
//    }

//    private void HandleStateChanged(InteractableStateChangeArgs args)
//    {
//        // Trigger when grabbed (Select state)
//        if (args.NewState == InteractableState.Select)
//        {
//            SaveState();
//        }
//    }

//    private void SaveState()
//    {
        
//    }
//}