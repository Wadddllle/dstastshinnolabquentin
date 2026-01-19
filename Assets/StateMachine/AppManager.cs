using UnityEngine;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;

    [Header("Data")]
    public SessionData currentSession = new SessionData();

    [Header("Scene Roots (The Parents)")]
    public GameObject InstructorRoot;
    public GameObject TraineeRoot;
    public GameObject AARRoot;

    [Header("Script References (For Data/Logic only)")]
    // We still need these to set variables (like filenames), 
    // but we WON'T use them for SetActive().
    public TacticalGrid tacticalGrid;
    public MapGenerator mapGenerator;
    public GridRecorder gridRecorder;
    public AAR_Visualizer aarVisualizer;

    [Header("Exceptions (Head-Mounted)")]
    // Since this is childed to the Camera, it can't be under TraineeRoot.
    // We must toggle it manually.
    public VisionScanner visionScanner;

    // State Machine internals
    public BaseState _currentState { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ensure everything is off to start
        InstructorRoot.SetActive(false);
        TraineeRoot.SetActive(false);
        AARRoot.SetActive(false);

        ChangeState(new InstructorState());
    }

    void Update()
    {
        if (_currentState != null) _currentState.UpdateState();
    }

    public void ChangeState(BaseState newState)
    {
        if (_currentState != null) _currentState.ExitState();
        _currentState = newState;
        _currentState.EnterState();
    }

    // --- THE MAGIC HELPER FUNCTION ---
    public void SetActiveRoot(GameObject rootToOn)
    {
        // 1. Turn everything OFF
        InstructorRoot.SetActive(false);
        TraineeRoot.SetActive(false);
        AARRoot.SetActive(false);

        // 2. Turn the requested one ON
        if (rootToOn != null)
        {
            rootToOn.SetActive(true);
        }
    }
}