using KevinIglesias;
using Oculus.Interaction;
using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.AI;

public class HostageAI : MonoBehaviour
{
    public EnemyConfig config;

    [Header("References")]
    private Transform player;
    public Transform eyePoint;
    public LayerMask losMask;
    private NavMeshAgent agent;
    private HumanSoldierController soldier;
    private GameObject[] enemies;

    [Header("Ranges")]
    public float detectionRange;
    public float eyeRange;
    private float distance;

    public enum State { Idle, Run, Crouch, Dead }
    public State state = State.Idle;

    [SerializeField] private bool isActive;
    [SerializeField] private bool isAfraid;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        agent = GetComponent<NavMeshAgent>();
        soldier = GetComponent<HumanSoldierController>();
    }

    void UpdateState()
    {
        bool shouldBeActive = AppManager.Instance.IsTraineeState();

        if (isActive == shouldBeActive)
            return;
        else
            isActive = shouldBeActive;
    }

    void Update()
    {
        UpdateState();

        /*if (!isActive || state == State.Dead)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            return;
        }
        else
            agent.isStopped = false;*/

        distance = Vector3.Distance(transform.position, player.position);
        float currentSpeed = agent.velocity.magnitude;
        Health hostage_healthState = gameObject.GetComponent<Health>();

        if (hostage_healthState.currentHealth <= 0f)
            Kill();
        
        switch (state)
        {
            case State.Idle:
                soldier.movement = SoldierMovement.NoMovement;
                soldier.action = SoldierAction.Idle;

                if (isAfraid)
                    state = State.Crouch;
                break;

            case State.Run:
                
                agent.SetDestination(-player.position);

                // Update movement based on current velocity
                if (currentSpeed < 0.1f)
                    soldier.movement = SoldierMovement.NoMovement;
                else if (currentSpeed < 2f)
                    soldier.movement = SoldierMovement.Walk;
                else
                    soldier.movement = SoldierMovement.Run;

                soldier.action = SoldierAction.Nothing;

                break;

            case State.Crouch:

                agent.ResetPath(); // stop moving
                soldier.movement = SoldierMovement.NoMovement; //by right supposed to be crouch but the animation doesnt come in the free package
                break;

        }
        
    }

    /// <summary>
    /// Call this to kill the enemy.
    /// </summary>
    public void Kill()
    {
        state = State.Dead;
        soldier.movement = SoldierMovement.NoMovement;
        soldier.action = SoldierAction.Death01;

        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.LogEvent("COLLATERAL", $"Accidentally killed: {gameObject.name}", transform.position); //by right location param is supposed to be where enemy got shot, but that is alr recorded in HIT in bullet script
        }

        Debug.Log($"Hostage Down: {gameObject.name}");

    }

    
    public void AlertByGunshot()
    {
        if (state == State.Dead) 
            return;

        else if (state == State.Idle && distance <= 20f) //hardcoded!
        {
            isAfraid = true;
        }
    }

    public void ApplyConfig(EnemyConfig config)
    {
        detectionRange = config.detectionRange;
        eyeRange = config.eyeRange;

        agent.speed = config.movementSpeed;
        agent.angularSpeed = config.turningSpeed;
    }
}
