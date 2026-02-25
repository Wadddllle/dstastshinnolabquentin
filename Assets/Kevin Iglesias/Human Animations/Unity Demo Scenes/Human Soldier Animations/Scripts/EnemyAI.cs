using KevinIglesias;
using Oculus.Interaction;
using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public EnemyConfig config;

    [Header("References")]
    private Transform player_target;
    public Transform eyePoint;
    public LayerMask losMask;
    private NavMeshAgent agent;
    private HumanSoldierController soldier;
    public Renderer mainRenderer;

    [Header("Ranges")]
    public float detectionRange;
    public float eyeRange;
    public float attackRange;
    private float distance;

    [Header("Bullet")]
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float maxBulletSpreadAngle = 2f;
    public float shot_cooldown;
    public ParticleSystem muzzleflash;

    [Header("Repositioning")]
    public float repositionRadius = 3f;
    public float repositionCooldown = 1f;

    [Header("Reaction")]
    public float reactionTime_offGuard;
    public float reactionTime_aware;
    [SerializeField] private bool reacting;
    [SerializeField] private bool isAware = false; //is the enemy aware of the player's presence? (important for setting reaction time)

    public enum State { Idle, Chase, Attack, Reposition, Dead }
    public State state = State.Idle;
    private float lastAttackTime;
    private float lastRepositionTime;
    private Vector3 repositionTarget;
    [SerializeField] private bool isActive;
    

    void Start()
    {
        player_target = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        soldier = GetComponent<HumanSoldierController>();
    }

    void UpdateState()
    {
        bool shouldBeActive = AppManager.Instance.IsTraineeState();

        if (isActive == shouldBeActive)
            return;
        isActive = shouldBeActive;
    }

    void Update()
    {
        UpdateState();
        if (!isActive || state == State.Dead)
        {
            if (agent != null)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                return;
            }
        }
        else
            agent.isStopped = false;

        distance = Vector3.Distance(transform.position, player_target.position);
        float currentSpeed = agent.velocity.magnitude;
        Health enemy_healthState = gameObject.GetComponent<Health>();

        if (enemy_healthState.currentHealth <= 0f)
            Kill();
        
        switch (state)
        {
            case State.Idle:
                soldier.movement = SoldierMovement.NoMovement;
                soldier.action = SoldierAction.Nothing;

                if (distance <= detectionRange && !isAware)
                {
                    isAware = true;
                    StartCoroutine(ReactionDelay(reactionTime_offGuard));
                }
                else if (distance <= detectionRange && isAware)
                {
                    StartCoroutine(ReactionDelay(reactionTime_aware));
                }
                break;

            case State.Chase:
                
                agent.SetDestination(player_target.position);

                // Update movement based on current velocity
                if (currentSpeed < 0.1f)
                    soldier.movement = SoldierMovement.NoMovement;
                else if (currentSpeed < 2f)
                    soldier.movement = SoldierMovement.Walk;
                else
                    soldier.movement = SoldierMovement.Run;

                soldier.action = SoldierAction.Nothing;

                if (distance <= attackRange && LineOfSight())
                    state = State.Attack;
                else if (distance > detectionRange)
                    state = State.Idle;
                break;

            case State.Attack:

                agent.ResetPath(); // stop moving
                soldier.movement = SoldierMovement.NoMovement;

                LookAtPlayer();
                EnemyShoot();

                if (distance > attackRange || !LineOfSight())
                    state = State.Chase;

                break;

            case State.Reposition:

                if (currentSpeed < 0.1f)
                    soldier.movement = SoldierMovement.NoMovement;
                else if (currentSpeed < 2f)
                    soldier.movement = SoldierMovement.Walk;
                else
                    soldier.movement = SoldierMovement.Run;

                soldier.action = SoldierAction.Nothing;

                LookAtPlayer();

                if (LineOfSight())
                {
                    //agent.ResetPath();
                    state = State.Attack;
                }

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
        soldier.action = SoldierAction.Death02;

        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.LogEvent("KILL", $"Neutralized: {gameObject.name}", transform.position); //by right location param is supposed to be where enemy got shot, but that is alr recorded in HIT in bullet script
        }

        Debug.Log($"Target Down: {gameObject.name}");

    }

    void BulletSpawner(float i = 0f, float j = 0f, float k = 0f)
    {
        //bulletSpawnPoint.Rotate(i, j, k, Space.Self);


        var bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        //give the bullet a random variance spread in a cone of angle = maxBulletSpreadAngle
        Vector3 spreadDir = Quaternion.Euler(
            Random.Range(-maxBulletSpreadAngle, maxBulletSpreadAngle),
            Random.Range(-maxBulletSpreadAngle,maxBulletSpreadAngle),
            0f) * bulletSpawnPoint.forward;

        bullet.GetComponent<Rigidbody>().linearVelocity = spreadDir.normalized * bulletSpeed;
        muzzleflash.Play();

        //bulletSpawnPoint.Rotate(-i, -j, -k, Space.Self);
    }

    void EnemyShoot()
    {
        if (Time.time > lastAttackTime + shot_cooldown)
        {
            // Trigger shooting once
            if (soldier.action != SoldierAction.Shoot01)
                soldier.action = SoldierAction.Shoot01;

            BulletSpawner();
            lastAttackTime = Time.time;
        }
    }

    bool LineOfSightFrom(Vector3 pos)
    {
        Vector3 targetPoint = player_target.position;
        Vector3 direction = (targetPoint - pos).normalized;
        bool los = false;

        if (Physics.Raycast(eyePoint.position, direction, out RaycastHit hit, eyeRange, losMask))
            los = hit.collider.CompareTag("Player");

        Debug.DrawRay(eyePoint.position, direction * eyeRange, los ? UnityEngine.Color.green : UnityEngine.Color.red);

        return los;
    }

    bool LineOfSight()
    {
        return LineOfSightFrom(eyePoint.position);
    }

    /*bool RepositionPoint(out Vector3 point)
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        Vector3 rightDirn = Vector3.Cross(Vector3.up, toPlayer);

        Vector3[] candidates =
        {
            transform.position + rightDirn * repositionRadius,
            transform.position - rightDirn * repositionRadius,
            transform.position + (toPlayer + rightDirn).normalized * repositionRadius,
            transform.position + (toPlayer - rightDirn).normalized * repositionRadius
        };

        foreach (var c in candidates)
        {
            if (!NavMesh.SamplePosition(c, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                continue;

            if (LineOfSightFrom(hit.position))
            {
                point = hit.position;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    } */

    void LookAtPlayer()
    {
        Vector3 lookPos = player_target.position - Vector3.up * 1.4f; //hardcoded!!
        transform.LookAt(lookPos);
    }
    public void AlertByGunshot()
    {
        if (state == State.Dead) 
            return;
        else if (state == State.Idle && distance <= 10f) //hardcoded!
        {
            isAware = true;
            StartCoroutine(ReactionDelay(reactionTime_offGuard)); //alr changes state to chase here
        }
            

    }
    IEnumerator ReactionDelay(float delay)
    {
        if (reacting) 
            yield 
        break;

        reacting = true;

       yield return new WaitForSeconds(delay);

        state = State.Chase;
        reacting = false;
    }

    public void ApplyConfig(EnemyConfig config)
    {
        detectionRange = config.detectionRange;
        eyeRange = config.eyeRange;
        attackRange = config.attackRange;
        shot_cooldown = config.shot_cooldown;
        reactionTime_offGuard = config.reactionTime_offGuard;
        reactionTime_aware = config.reactionTime_aware;

        agent.speed = config.movementSpeed;
        agent.angularSpeed = config.turningSpeed;
    }
}
