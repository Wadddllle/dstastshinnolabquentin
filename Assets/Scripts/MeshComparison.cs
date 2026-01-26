using UnityEngine;

public class ComparisonSwitcher : MonoBehaviour
{
    [Header("Comparison Objects")]
    public GameObject staticObject;
    public GameObject dynamicObject;

    void Start()
    {
        // Set initial state: Static ON, Dynamic OFF
        if (staticObject != null) staticObject.SetActive(true);
        if (dynamicObject != null) dynamicObject.SetActive(false);
    }

    void Update()
    {
        // OVRInput.Button.One corresponds to the "A" button on Quest controllers
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            ToggleComparison();
        }
    }

    void ToggleComparison()
    {
        if (staticObject != null && dynamicObject != null)
        {
            // Simply flip the current active state
            bool isStaticActive = staticObject.activeSelf;

            staticObject.SetActive(!isStaticActive);
            dynamicObject.SetActive(isStaticActive);

            Debug.Log("Swapped! Static: " + staticObject.activeSelf + " | Dynamic: " + dynamicObject.activeSelf);
        }
    }
}