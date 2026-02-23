using UnityEngine;
using UnityEngine.Rendering;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;
    public static CanvasLogic canvasLogic; // Add this if you haven't yet

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
    public AAR_ReportCard aarReportCard;

    [Header("Exceptions")]
    // 1. Vision Scanner (Head)
    public VisionScanner visionScanner;
    // 2. The Gun (Right Hand Controller) - NEW
    //public RaycastWeapon raycastWeapon;
    public GlobalPathfinder globalPathfinder;
    public GameObject gunPivotRoot;


    // State Machine internals
    public BaseState _currentState { get; private set; }
    public event System.Action OnStateChanged;

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

        if (visionScanner) visionScanner.enabled = false;
        //if (raycastWeapon) raycastWeapon.enabled = false;
        if (gunPivotRoot) gunPivotRoot.SetActive(false); // Ensure gun starts hidden


        ChangeState(new InstructorState());
    }

    void Update()
    {
        if (_currentState != null) 
            _currentState.UpdateState();
    }

    public void ChangeState(BaseState newState)
    {
        if (_currentState != null) 
            _currentState.ExitState();
        _currentState = newState;
        _currentState.EnterState();

        //OnStateChanged?.Invoke();
    }

    // --- THE MAGIC HELPER FUNCTION ---
    public void SetActiveRoot(GameObject rootToOn)
    {
        // 1. Turn everything OFF
        InstructorRoot.SetActive(false);
        TraineeRoot.SetActive(false);
        AARRoot.SetActive(false);

        if (visionScanner != null) visionScanner.gameObject.SetActive(false);
        //if (raycastWeapon != null) raycastWeapon.gameObject.SetActive(false);
        if (gunPivotRoot != null) gunPivotRoot.SetActive(false);



        // 2. Turn the requested one ON
        if (rootToOn != null)
        {
            rootToOn.SetActive(true);           
        }
    }
    public void SetEnemyAI(bool enable)
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (var enemyAI in enemies)
        {
            enemyAI.enabled = enable;  

            var agent = enemyAI.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = !enable;
            }
        }
    }

    public bool IsTraineeState()
    {
        return _currentState is TraineeState;
    }

    public bool IsInstructorState()
    {
        return _currentState is InstructorState;
    }

    public bool IsAARState()
    {
        return _currentState is AARState;
    }
}