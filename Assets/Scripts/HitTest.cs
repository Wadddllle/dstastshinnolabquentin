using UnityEngine;

public class HitTest : MonoBehaviour
{
    // The color to change to when triggered
    public Color newColor = Color.blue;

    // Optional: The original color of the object
    private Color originalColor;

    // Reference to the object's Renderer component
    private Renderer objectRenderer;

    void Start()
    {
        // Get the Renderer component attached to this GameObject
        objectRenderer = GetComponent<Renderer>();

        // Store the original color of the material
        originalColor = objectRenderer.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger has the "projectile" tag
        if (other.CompareTag("bullet"))
        {
            // Change the material's color to the new color
            objectRenderer.material.color = newColor;
        }
    }

    // Optional: If you want the color to revert when the projectile leaves the trigger
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that exited the trigger has the "projectile" tag
        if (other.CompareTag("bullet"))
        {
            // Revert the material's color to the original color
            objectRenderer.material.color = originalColor;
        }
    }
}