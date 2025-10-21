using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyNavMeshAI : MonoBehaviour
{
    public enum AIState { Idle, Patrolling, Pursuing, Lurking, Attacking }
    public enum CombatStyle { Melee, Ranged }

    [Header("State Machine")]
    [SerializeField] private AIState currentState = AIState.Idle;

    [Header("Combat Style")]
    public CombatStyle combatStyle = CombatStyle.Melee;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 3.5f;
    public float patrolStoppingDistance = 1f;

    [Header("Pursuit Settings")]
    public float pursuitSpeed = 7f;
    public float engagementDistance = 15f;

    [Header("Melee Lurking Settings")]
    public float meleeLurkSpeed = 4f;
    public float meleeLurkRadius = 10f;
    public float meleeRepositionTime = 3f;

    [Header("Ranged Circling Settings")]
    public float rangedCirclingSpeed = 5f;
    public float desiredRangedDistance = 10f;
    public float directionChangeInterval = 4f;

    [Header("Detection Settings")]
    public string playerTag = "Player";
    public float maxDetectionRadius = 25f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask obstacleMask;
    public float checkInterval = 0.2f;

    [Header("Animation Settings")]
    public string animatorSpeedParameter = "MovementSpeed";
    public string animatorVelocityXParameter = "VelocityX";
    public string animatorVelocityZParameter = "VelocityZ";

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private Transform playerTarget;
    private EnemyAttack enemyAttack;
    private int currentPatrolIndex;
    private float timeInCurrentState = 0f;
    private float timeSinceLastMeleeReposition = 0f;
    private int circlingDirection = 1;
    private bool justAttacked = false;
    private int animatorSpeedParamHash;
    private int animatorAIStateParamHash;
    private int animatorVelocityXHash;
    private int animatorVelocityZHash;


    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyAttack = GetComponent<EnemyAttack>();
    }

    void Start()
    {
        animatorSpeedParamHash = Animator.StringToHash(animatorSpeedParameter);
        animatorAIStateParamHash = Animator.StringToHash("AIState");
        animatorVelocityXHash = Animator.StringToHash(animatorVelocityXParameter);
        animatorVelocityZHash = Animator.StringToHash(animatorVelocityZParameter);
        currentPatrolIndex = 0;
    }

    public void ActivateAI()
    {
        SetState(AIState.Patrolling);
        InvokeRepeating(nameof(CheckConditions), 0, checkInterval);
    }

    void Update()
    {
        GameObject playerObject = GameObject.FindWithTag(playerTag);
        if (playerObject != null) playerTarget = playerObject.transform;
        if (currentState == AIState.Idle) return;

        if (playerTarget == null)
        {
            if (currentState != AIState.Patrolling)
            {
                SetState(AIState.Patrolling);
            }
        }

        timeInCurrentState += Time.deltaTime;

        switch (currentState)
        {
            case AIState.Patrolling: Patrol(); break;
            case AIState.Pursuing: Pursue(); break;
            case AIState.Lurking: Lurk(); break;
            case AIState.Attacking: Attack(); break;
        }
        UpdateAnimator();
    }

    private void SetState(AIState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        timeInCurrentState = 0f;

        switch (currentState)
        {
            case AIState.Idle:
                animator.SetInteger("AIState", -1);
                navMeshAgent.isStopped = true;
                break;
            case AIState.Patrolling:
                animator.SetInteger("AIState", 0);
                navMeshAgent.isStopped = false;
                navMeshAgent.speed = patrolSpeed;
                navMeshAgent.stoppingDistance = patrolStoppingDistance;
                if (patrolPoints != null && patrolPoints.Length > 0)
                    navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                break;
            case AIState.Pursuing:
                animator.SetInteger("AIState", 1);
                navMeshAgent.isStopped = false;
                navMeshAgent.speed = pursuitSpeed;
                navMeshAgent.stoppingDistance = engagementDistance;
                break;
            case AIState.Lurking:
                animator.SetInteger("AIState", 2);
                navMeshAgent.isStopped = false;
                navMeshAgent.speed = (combatStyle == CombatStyle.Melee) ? meleeLurkSpeed : rangedCirclingSpeed;
                navMeshAgent.stoppingDistance = 0;
                circlingDirection = (Random.value > 0.5f) ? 1 : -1;
                timeSinceLastMeleeReposition = 0f;
                break;
            case AIState.Attacking:
                animator.SetInteger("AIState", 3);
                navMeshAgent.ResetPath();
                break;
        }
    }

    // ... all other methods remain the same ...
    private void CheckConditions()
    {
        if (playerTarget == null || currentState == AIState.Attacking || currentState == AIState.Idle) return;
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
                else if (!justAttacked && enemyAttack.CanAttack && distanceToPlayer <= enemyAttack.attackRange)
                {
                    SetState(AIState.Attacking);
                }
                break;
        }
    }
    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            navMeshAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }
    private void Pursue()
    {
        if (playerTarget != null) navMeshAgent.SetDestination(playerTarget.position);
    }
    private void Lurk()
    {
        if (playerTarget == null) return;
        FaceTarget(playerTarget.position);
        if (combatStyle == CombatStyle.Melee) { LurkMelee(); }
        else { LurkRanged(); }
    }
    private void LurkMelee()
    {
        if (justAttacked)
        {
            justAttacked = false;
            ForceMeleeReposition();
            return;
        }
        timeSinceLastMeleeReposition += Time.deltaTime;
        if (timeSinceLastMeleeReposition >= meleeRepositionTime)
        {
            ForceMeleeReposition();
        }
    }
    private void Attack()
    {
        if (playerTarget == null) { SetState(AIState.Patrolling); return; }
        FaceTarget(playerTarget.position);
        if (timeInCurrentState < Time.deltaTime * 2)
        {
            enemyAttack.PerformAttack(playerTarget);
        }
        if (timeInCurrentState >= enemyAttack.attackDuration)
        {
            if (combatStyle == CombatStyle.Melee) { justAttacked = true; }
            SetState(AIState.Lurking);
        }
    }
    private void ForceMeleeReposition()
    {
        if (playerTarget == null) return;
        timeSinceLastMeleeReposition = 0f;
        float repositionRadius = enemyAttack.attackRange * 0.9f;
        Vector2 randomPoint2D = Random.insideUnitCircle * repositionRadius;
        Vector3 randomPoint3D = new Vector3(playerTarget.position.x + randomPoint2D.x, transform.position.y, playerTarget.position.z + randomPoint2D.y);
        if (NavMesh.SamplePosition(randomPoint3D, out NavMeshHit hit, repositionRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }
    private void LurkRanged()
    {
        if (playerTarget == null) return;
        float timeStuck = 0f;
        if (navMeshAgent.hasPath && navMeshAgent.velocity.sqrMagnitude < 0.1f) timeStuck += Time.deltaTime;
        else timeStuck = 0f;
        if (timeStuck > 0.75f)
        {
            circlingDirection *= -1;
            timeStuck = 0f;
        }
        if (timeInCurrentState > directionChangeInterval)
        {
            timeInCurrentState = 0;
            circlingDirection = (Random.value > 0.5f) ? 1 : -1;
        }
        Vector3 directionToPlayer = transform.position - playerTarget.position;
        Vector3 idealPosition = playerTarget.position + directionToPlayer.normalized * desiredRangedDistance;
        Vector3 perpendicularDirection = Vector3.Cross(directionToPlayer, Vector3.up).normalized * circlingDirection;
        Vector3 finalDestination = idealPosition + perpendicularDirection * 5f;
        navMeshAgent.SetDestination(finalDestination);
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
        float speed = (currentState == AIState.Attacking || currentState == AIState.Idle) ? 0f : navMeshAgent.velocity.magnitude;
        Vector3 localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
        float velocityX = localVelocity.x;
        float velocityZ = localVelocity.z;
        animator.SetFloat(animatorSpeedParamHash, speed);
        animator.SetFloat(animatorVelocityXHash, velocityX);
        animator.SetFloat(animatorVelocityZHash, velocityZ);
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
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, maxDetectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, engagementDistance);
        var attackComponent = GetComponent<EnemyAttack>();
        if (attackComponent != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackComponent.attackRange);
            if (combatStyle == CombatStyle.Ranged)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, desiredRangedDistance);
            }
        }
        Vector3 viewAngleA = DirectionFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirectionFromAngle(-viewAngle / 2, false);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * maxDetectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * maxDetectionRadius);
    }
    private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}