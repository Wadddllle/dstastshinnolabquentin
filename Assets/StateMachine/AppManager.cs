using UnityEngine;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;

    [Header("Data")]
    public SessionData currentSession = new SessionData();

    [Header("Core Systems")]
    public TacticalGrid tacticalGrid;
    public MapGenerator mapGenerator;


    [Header("Instructor Tools")]
    public InstructorPlacementTool instructorTool;


    [Header("Trainee Tools")]
    public buttontest projectileLauncher;
    public GridRecorder gridRecorder;

    [Header("AAR Tools")]
    public AAR_Visualizer aarVisualizer;
    public GridReplayer gridReplayer;
    public VisionScanner visionScanner; // <--- ADD THIS




    // State Machine internals
    private BaseState _currentState;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private float debugInterval = 5f; // Print every 5 seconds
    private float _nextDebugTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ChangeState(new InstructorState());
    }

    void Update()
    {
        if (_currentState != null)
        {
            _currentState.UpdateState();

            // Periodic debug print
            if (showDebugLogs && Time.time >= _nextDebugTime)
            {
                Debug.Log($"<color=cyan>[AppManager]</color> Currently in: <b>{_currentState.GetType().Name}</b>");
                _nextDebugTime = Time.time + debugInterval;
            }
        }
    }

    public void ChangeState(BaseState newState)
    {
        if (_currentState != null)
        {
            if (showDebugLogs) Debug.Log($"<color=orange>[State Exit]</color> Leaving: {_currentState.GetType().Name}");
            _currentState.ExitState();
        }

        _currentState = newState;

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[State Enter]</color> Entered: <b>{_currentState.GetType().Name}</b>");
        }

        _currentState.EnterState();

        // Reset the periodic timer so it doesn't fire immediately after a change
        _nextDebugTime = Time.time + debugInterval;
    }
}