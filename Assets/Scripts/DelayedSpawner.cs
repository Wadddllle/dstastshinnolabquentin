using UnityEngine;

public class DelayedSpawner : MonoBehaviour
{
    [Tooltip("How far in front of the player to spawn the object.")]
    [SerializeField] private float spawnDistance = 1.5f;

    void Awake()
    {
        // Immediately disable the object so it doesn't fall through the world.
        gameObject.SetActive(false);

        // Subscribe to the game start event.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStarted += OnGameStarted;
        }
    }

    // This function will be called by the GameStateManager when play begins.
    private void OnGameStarted()
    {
        // Find the player's head (the main camera).
        Transform playerHead = Camera.main.transform;

        // Calculate a spawn position in front of and slightly above the player.
        Vector3 forward = new Vector3(playerHead.forward.x, 0, playerHead.forward.z).normalized;
        Vector3 spawnPosition = playerHead.position + (forward * spawnDistance) + (Vector3.up * 0.5f);

        // Move this object to the calculated position.
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;

        // Finally, activate the object. Its physics will now take over.
        gameObject.SetActive(true);
        Debug.Log($"{name} has been spawned at {spawnPosition}");
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStarted -= OnGameStarted;
        }
    }
}