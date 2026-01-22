using UnityEngine;

public class TargetBehavior : MonoBehaviour
{
    // Assign these in Inspector
    public Renderer meshRenderer;
    public Material aliveMaterial;
    public Material deadMaterial;

    private bool _isDead = false;

    // Called by SessionManager when starting Trainee Mode
    public void ResetTarget()
    {
        _isDead = false;
        if (meshRenderer && aliveMaterial) meshRenderer.material = aliveMaterial;
    }

    // Called by the Gun
    public void HitByBullet(Vector3 hitPoint)
    {
        if (_isDead) return;

        _isDead = true;

        if (meshRenderer && deadMaterial) meshRenderer.material = deadMaterial;

        // --- THE FIX ---
        // Use GridRecorder.Instance instead of Manager.gridRecorder
        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.LogEvent("KILL", $"Neutralized: {gameObject.name}", hitPoint);
        }

        Debug.Log($"Target Down: {gameObject.name}");
    }
}