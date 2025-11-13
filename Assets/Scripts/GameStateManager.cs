using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    // Singleton: A globally accessible instance of this manager.
    public static GameStateManager Instance { get; private set; }

    public enum GameState { Scanning, Playing }
    public GameState CurrentState { get; private set; }

    // Event: Other scripts can "listen" for when the game starts.
    public event System.Action OnGameStarted;

    void Awake()
    {
        // Set up the Singleton instance.
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    void Start()
    {
        // Begin the game in the Scanning state.
        EnterScanningState();
    }

    public void EnterScanningState()
    {
        CurrentState = GameState.Scanning;
        Debug.Log("Entered SCANNING state. Look around to map your room.");
    }


    [ContextMenu("Enter Playing State")]
    public void EnterPlayingState()
    {
        if (CurrentState != GameState.Scanning) return; // Prevent re-starting

        CurrentState = GameState.Playing;
        Debug.Log("Entered PLAYING state. Physics and gameplay are now active!");

        // This is the broadcast message. Any script listening will now run its function.
        OnGameStarted?.Invoke();
    }

    void Update()
    {
        //// Use a controller button in your final game. For now, a keyboard key is easy.
        //// You could also hook this up to a UI button.
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    EnterPlayingState();
        //}
    }
}