using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyNavMeshAI : MonoBehaviour
{
    public enum AIState { Patrolling, Pursuing, Lurking }

    [Header("State Machine")]
    [SerializeField] private AIState currentState = AIState.Patrolling;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 3.5f;

    [Header("Pursuit Settings")]
    public float pursuitSpeed = 7f;
    public float engagementDistance = 15f;

    [Header("Combat & Evasion (Lurking)")]
    public float lurkSpeed = 4f;
    public float lurkRadius = 10f;
    public float lurkPointChangeTime = 3f;

    [Header("Detection Settings")]
    public string playerTag = "Player";
    public float maxDetectionRadius = 25f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask obstacleMask;
    public float checkInterval = 0.2f;

    [Header("Animation Settings")]
    public string animatorSpeedParameter = "MovementSpeed";

    // --- Component & Target References ---
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private Transform playerTarget;
    private EnemyAttack enemyAttack;
    private int currentPatrolIndex;

    private float timeSinceLastLurkChange = 0f;
    private int animatorSpeedParamHash;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyAttack = GetComponent<EnemyAttack>(); 

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogError($"EnemyAI: Cannot find GameObject with tag '{playerTag}'. AI will not function correctly.", this);
        }
    }

    void Start()
    {
        animatorSpeedParamHash = Animator.StringToHash(animatorSpeedParameter);

        currentPatrolIndex = 0;
        SetState(AIState.Patrolling);

        InvokeRepeating(nameof(CheckConditions), 0, checkInterval);
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Patrolling: Patrol(); break;
            case AIState.Pursuing: Pursue(); break;
            case AIState.Lurking: Lurk(); break;
        }

        UpdateAnimator();
    }

    private void SetState(AIState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case AIState.Patrolling:
                navMeshAgent.speed = patrolSpeed;
                navMeshAgent.stoppingDistance = 0;
                if (patrolPoints.Length > 0)
                {
                    navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                break;

            case AIState.Pursuing:
                navMeshAgent.speed = pursuitSpeed;
                navMeshAgent.stoppingDistance = engagementDistance;
                break;

            case AIState.Lurking:
                navMeshAgent.speed = lurkSpeed;
                navMeshAgent.stoppingDistance = 0;
                timeSinceLastLurkChange = 0;
                break;
        }
    }

    private void CheckConditions()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool canSeePlayer = CanSeePlayer(distanceToPlayer);

        if (!canSeePlayer && distanceToPlayer > maxDetectionRadius)
        {
            SetState(AIState.Patrolling);
            return;
        }

        switch (currentState)
        {
            case AIState.Patrolling:
                if (canSeePlayer) SetState(AIState.Pursuing);
                break;
            case AIState.Pursuing:
                if (!canSeePlayer) SetState(AIState.Patrolling);
                else if (distanceToPlayer <= engagementDistance) SetState(AIState.Lurking);
                break;
            case AIState.Lurking:
                if (distanceToPlayer > engagementDistance * 1.2f) SetState(AIState.Pursuing);
                break;
        }
    }

    private void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    private void Pursue()
    {
        if (playerTarget != null)
        {
            navMeshAgent.SetDestination(playerTarget.position);
        }
    }

    private void Lurk()
    {
        FaceTarget(playerTarget.position);

        // --- DELEGATED ATTACK LOGIC ---
        if (enemyAttack.CanAttack && Vector3.Distance(transform.position, playerTarget.position) <= enemyAttack.attackRange)
        {
            navMeshAgent.ResetPath();
            enemyAttack.PerformAttack();
            return;
        }

        // --- Evasion ---
        timeSinceLastLurkChange += Time.deltaTime;
        if (timeSinceLastLurkChange >= lurkPointChangeTime)
        {
            timeSinceLastLurkChange = 0;

            Vector2 randomPoint2D = Random.insideUnitCircle * lurkRadius;
            Vector3 randomPoint3D = new Vector3(playerTarget.position.x + randomPoint2D.x,
                                                transform.position.y,
                                                playerTarget.position.z + randomPoint2D.y);

            if (NavMesh.SamplePosition(randomPoint3D, out NavMeshHit hit, lurkRadius, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        if (direction == Vector3.zero) return;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * navMeshAgent.angularSpeed);
    }

    private void UpdateAnimator()
    {
        float speed = navMeshAgent.velocity.magnitude;
        animator.SetFloat(animatorSpeedParamHash, speed);
    }

    private bool CanSeePlayer(float distanceToPlayer)
    {
        if (playerTarget == null) return false;
        if (distanceToPlayer > maxDetectionRadius) return false;

        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > viewAngle / 2) return false;
        if (Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask)) return false;

        return true;
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection and engagement gizmos from the main script
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, maxDetectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, engagementDistance);

        var attackComponent = GetComponent<EnemyAttack>();
        if (attackComponent != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackComponent.attackRange);
        }

        Vector3 viewAngleA = DirectionFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirectionFromAngle(viewAngle / 2, false);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * maxDetectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * maxDetectionRadius);
    }

    private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
