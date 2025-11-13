using UnityEngine;
using Meta.XR.MRUtilityKit;

public class MRUKAnchorManager : MonoBehaviour
{
    [Tooltip("The object that contains all your virtual content (like the Chunk Manager).")]
    [SerializeField] private Transform _worldContent;

    [Header("Idealized Floor Settings")]
    [Tooltip("Check this to create a simple, flat plane for floor collision.")]
    [SerializeField] private bool _createIdealizedFloor = true;

    [Tooltip("The size of the floor plane to create, in meters.")]
    [SerializeField] private float _floorSize = 20.0f;

    [Header("Debugging & Physics")]
    [Tooltip("The VISUAL material for the floor plane. If left empty (None), the floor will be invisible but still have a collider.")]
    [SerializeField] private Material _floorVisualMaterial;

    [Tooltip("The PHYSICS material for the floor collider (optional). Defines friction and bounciness.")]
    [SerializeField] private PhysicsMaterial _floorPhysicMaterial;

    public static Transform AnchorParent { get; private set; }

    private MeshRenderer _floorRenderer;

    void Start()
    {
        if (_worldContent == null)
        {
            Debug.LogError("World Content transform is not assigned in the Inspector!", this);
            return;
        }

        MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);
    }

    private void OnDestroy()
    {
        if (MRUK.Instance)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
        }
    }

    private void OnSceneLoaded()
    {
        Debug.Log("MRUK SceneLoadedEvent fired. Searching for the floor anchor...");

        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null)
        {
            Debug.LogError("MRUK reports that no room was found. Cannot establish an anchor.");
            return;
        }

        MRUKAnchor floorAnchor = currentRoom.FloorAnchor;
        if (floorAnchor != null)
        {
            AnchorParent = floorAnchor.transform;

            Debug.Log("<color=green>SUCCESS: Found MRUK Floor Anchor. Parenting world content to it.</color>");
            _worldContent.SetParent(AnchorParent, worldPositionStays: true);

            if (_createIdealizedFloor)
            {
                CreateIdealizedFloor(floorAnchor);
            }
        }
        else
        {
            Debug.LogError("CRITICAL: MRUK found a room, but could not identify a floor anchor within it.");
        }
    }

    private void CreateIdealizedFloor(MRUKAnchor floorAnchor)
    {
        GameObject floorObject = new GameObject("Idealized_Floor");

        // --- THIS IS THE FIX ---

        // 1. Set the floor's POSITION in world space to match the anchor's position.
        floorObject.transform.position = floorAnchor.transform.position;

        // 2. Set the floor's ROTATION in world space to be perfectly horizontal (no rotation).
        floorObject.transform.rotation = Quaternion.identity;

        // 3. NOW, parent it to the anchor. The 'true' argument tells it to
        //    keep its new world position/rotation instead of snapping to the parent's.
        //    This gives us the drift correction without inheriting the bad rotation.
        floorObject.transform.SetParent(floorAnchor.transform, true);

        // --- END FIX ---

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.SetParent(floorObject.transform, false);
        plane.transform.localScale = Vector3.one * (_floorSize / 10.0f);

        _floorRenderer = plane.GetComponent<MeshRenderer>();

        if (_floorVisualMaterial != null)
        {
            _floorRenderer.material = _floorVisualMaterial;
        }
        else
        {
            // If no material, disable the renderer but don't destroy it.
            _floorRenderer.enabled = false;
        }

        var collider = plane.GetComponent<Collider>();
        if (_floorPhysicMaterial != null)
        {
            collider.material = _floorPhysicMaterial;
        }
    }
    public void ToggleFloorVisibility()
    {
        if (_floorRenderer != null)
        {
            _floorRenderer.enabled = !_floorRenderer.enabled;
        }
    }
}