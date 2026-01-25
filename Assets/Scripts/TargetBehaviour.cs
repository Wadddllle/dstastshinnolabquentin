using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Drag the soldier's Renderer here. (SkinnedMeshRenderer for characters, MeshRenderer for cubes)")]
    public Renderer mainRenderer;

    public Material aliveMaterial;
    public Material deadMaterial;

    [Tooltip("Optional: Drag Animator here if the soldier has animations")]
    public Animator soldierAnimator;

    private bool _isDead = false;

    // Called by your Game Manager to reset the enemy
    public void ResetTarget()
    {
        _isDead = false;

        // Reset Material
        if (mainRenderer && aliveMaterial)
            mainRenderer.material = aliveMaterial;

        // Reset Animation (if you add animations later)
        if (soldierAnimator != null)
        {
            soldierAnimator.enabled = true; // Re-enable animations
        }
    }

    // Called by BulletCollision.cs
    public void HitByBullet(Vector3 hitPoint)
    {
        if (_isDead) return;

        _isDead = true;

        // --- 1. Visual Feedback ---
        if (mainRenderer && deadMaterial)
        {
            mainRenderer.material = deadMaterial;
        }

        // --- 2. Handle 3D Model Death ---
        // If it's a soldier, you might want to disable the animator so it stops playing "Idle"
        // or enable ragdoll physics here. For now, we'll just stop the animator.
        if (soldierAnimator != null)
        {
            soldierAnimator.enabled = false;
        }

        // --- 3. Log to GridRecorder ---
        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.LogEvent("KILL", $"Neutralized: {gameObject.name}", hitPoint);
        }

        Debug.Log($"Target Down: {gameObject.name}");
    }
}