using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyNavMeshAI : MonoBehaviour
{
    public enum AIState { Idle, Patrolling, Pursuing, Lurking, Telegraphing, Attacking, Repositioning, Fleeing, TelegraphingSpit }
    public enum CombatStyle { Melee, Ranged }

    [Header("State Machine")]
    [SerializeField] private AIState currentState = AIState.Idle;

    [Header("Combat Style")]
    public CombatStyle combatStyle = CombatStyle.Melee;

    [Header("Attack Settings")]
    public float telegraphDuration = 0.75f;
    private bool isAttacking = false;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 3.5f;
    public float patrolStoppingDistance = 1f;

    [Header("Pursuit Settings")]
    public float pursuitSpeed = 7f;
    public float engagementDistance = 15f;

    [Header("Melee Lurking Settings")]
    public float meleeCirclingSpeed = 4.5f;
    public float meleeCirclingDistance = 6f;
    public float meleeDirectionChangeInterval = 1.5f;
    public float meleeDashInterval = 2f;
    public float meleeDashSpeed = 15f;
    public float dashApproachDuration = 0.75f;

    [Header("Melee Spit Attack")]
    [Tooltip("Arraste o componente 'EnemyAttack' secundário (do cuspe) para cá.")]
    public EnemyAttack spitAttackComponent;
    public float spitChance = 0.6f;
    public float spitCooldown = 5f;
    public float spitTelegraphDuration = 0.6f;
    private float timeSinceLastSpit = 0f;

    [Header("Ranged Circling Settings")]
    public float rangedCirclingSpeed = 5f;
    public float desiredRangedDistance = 10f;
    public float distanceDeadzone = 1.5f;
    public float repositionTickRate = 0.7f;
    public float rangedTacticChangeInterval = 4.0f;
    [Space]
    [Tooltip("Prioridade para manter distância vs. circular")]
    public float kitingWeight = 1.5f;
    public float strafeWeight = 1.0f;
    [Space]
    [Tooltip("Se o player estiver MAIS PERTO que isso, a IA entra em pânico e só tenta fugir.")]
    public float playerPanicDistance = 3.0f;
    [Tooltip("A que distância a IA 'vê' a parede para tentar desviar.")]
    public float wallAvoidDistance = 1.5f;
    public float fleeSpeed = 6.0f;

    [Header("Reposition Settings")]
    public float repositionSpeed = 4f;
    public float repositionDistance = 8f;
    public float repositionDuration = 2.5f;

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
    private float timeUntilNextRangedDecision = 0f;
    private float timeSinceLastDash = 0f;
    private int circlingDirection = 1;
    private bool justAttacked = false;
    private int animatorSpeedParamHash;
    private int animatorAIStateParamHash;
    private int animatorVelocityXHash;
    private int animatorVelocityZHash;
    private int animatorIsDashingHash;
    private bool isDashing = false;
    private float timeStuck = 0f;
    private float timeUntilMeleeDirectionChange = 0f;
    private float timeUntilRangedMoveTick = 0f; 
    private float timeUntilTacticChange = 0f;
    private float currentKiteWeight;
    private float currentStrafeWeight;
    private bool isPerformingSpit = false;
    private EnemyStats stats;
    private int animatorHealthHash;


    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyAttack = GetComponent<EnemyAttack>();
        stats = GetComponent<EnemyStats>();
    }

    void Start()
    {
        animatorSpeedParamHash = Animator.StringToHash(animatorSpeedParameter);
        animatorAIStateParamHash = Animator.StringToHash("AIState");
        animatorVelocityXHash = Animator.StringToHash(animatorVelocityXParameter);
        animatorVelocityZHash = Animator.StringToHash(animatorVelocityZParameter);
        animatorIsDashingHash = Animator.StringToHash("IsDashing");
        animatorHealthHash = Animator.StringToHash("HealthPercent");
        currentPatrolIndex = 0;
    }

    public void ActivateAI()
    {
        SetState(AIState.Patrolling);
        InvokeRepeating(nameof(CheckConditions), 0, checkInterval);
    }

    void Update()
    {
        if (isDashing) return;
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
            case AIState.Telegraphing: Telegraph(); break;
            case AIState.Attacking: Attack(); break;
            case AIState.Repositioning: Repositioning(); break;
            case AIState.Fleeing: Flee(); break;
            case AIState.TelegraphingSpit: TelegraphSpit(); break;
        }
        UpdateAnimator();
    }

    private void SetState(AIState newState)
    {
        if (isDashing) return;
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
                navMeshAgent.stoppingDistance = 2f;
                break;
            case AIState.Lurking:
                animator.SetInteger("AIState", 2);
                navMeshAgent.isStopped = false;

                if (combatStyle == CombatStyle.Melee)
                {
                    navMeshAgent.speed = meleeCirclingSpeed;
                    navMeshAgent.stoppingDistance = 0;
                    timeUntilMeleeDirectionChange = 0f;
                }
                else
                {
                    navMeshAgent.speed = rangedCirclingSpeed;
                    navMeshAgent.stoppingDistance = 0;
                    timeUntilRangedMoveTick = 0f;
                    timeUntilTacticChange = 0f;
                }
                break;
            case AIState.Attacking:
                animator.SetInteger("AIState", 3);
                navMeshAgent.ResetPath();
                break;

            case AIState.Telegraphing:
                animator.SetInteger("AIState", 4);
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                isAttacking = true;
                FaceTarget(playerTarget.position);
                break;

            case AIState.Repositioning:
                animator.SetInteger("AIState", 5);
                navMeshAgent.isStopped = false;
                navMeshAgent.speed = repositionSpeed;
                navMeshAgent.stoppingDistance = 0;

                if (playerTarget != null)
                {
                    Vector3 dirAwayFromPlayer = (transform.position - playerTarget.position).normalized;
                    if (dirAwayFromPlayer == Vector3.zero) dirAwayFromPlayer = transform.forward;

                    Vector3 targetPos = transform.position + dirAwayFromPlayer * repositionDistance;

                    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, repositionDistance, NavMesh.AllAreas))
                    {
                        navMeshAgent.SetDestination(hit.position);
                    }
                    else
                    {
                        navMeshAgent.SetDestination(targetPos);
                    }
                }
                break;

            case AIState.Fleeing:
                animator.SetInteger("AIState", 6);
                navMeshAgent.isStopped = false;
                navMeshAgent.speed = fleeSpeed;
                navMeshAgent.stoppingDistance = 0.5f;

                Transform closestPatrolPoint = GetClosestPatrolPoint();
                if (closestPatrolPoint != null)
                {
                    navMeshAgent.SetDestination(closestPatrolPoint.position);
                }
                else
                {
                    Debug.LogWarning("Fleeing state triggered but no patrol points found!", gameObject);
                    SetState(AIState.Patrolling);
                }
                break;
                
            case AIState.TelegraphingSpit:
                animator.SetInteger("AIState", 8);
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                isAttacking = true;
                isPerformingSpit = true;
                timeSinceLastSpit = 0f;
                FaceTarget(playerTarget.position);
                break;
        }
    }

    private IEnumerator DashReposition(Vector3 dashTarget, float dashDuration)
    {
        isDashing = true;
        animator.SetBool(animatorIsDashingHash, true);
        Debug.Log("Dash realizado");
        navMeshAgent.enabled = false;

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            float t = elapsedTime / dashDuration;

            transform.position = Vector3.Lerp(startPosition, dashTarget, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = dashTarget;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            navMeshAgent.enabled = true;
            navMeshAgent.Warp(hit.position);
        }
        else
        {
            Debug.LogError("Inimigo deu dash para fora da NavMesh!");
            navMeshAgent.enabled = true;
        }

        isDashing = false;
        animator.SetBool(animatorIsDashingHash, false);
    }

    private void CheckConditions()
    {
        if (isDashing) return;
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
                if (canSeePlayer)
                {
                    SetState(AIState.Pursuing);
                }
                break;

            case AIState.Pursuing:
                if (canSeePlayer && distanceToPlayer <= engagementDistance)
                {
                    SetState(AIState.Lurking);
                }
                break;

            case AIState.Lurking:
                if (distanceToPlayer > engagementDistance * 1.2f)
                {
                    SetState(AIState.Pursuing);
                }
                else if (!canSeePlayer)
                {
                    SetState(AIState.Pursuing);
                }
                else if (!isAttacking && enemyAttack.CanAttack && distanceToPlayer <= enemyAttack.attackRange)
                {
                    SetState(AIState.Telegraphing);
                }
                break;

            case AIState.Telegraphing:
                if (distanceToPlayer > enemyAttack.attackRange * 1.5f || !canSeePlayer)
                {
                    isAttacking = false;
                    SetState(AIState.Pursuing);
                }
                break;

            case AIState.Repositioning:
                break;
            case AIState.Fleeing:
                if (!canSeePlayer || distanceToPlayer > engagementDistance * 1.5f)
                {
                    SetState(AIState.Pursuing);
                }
                break;
        }
    }
    private void Patrol()
    {
        if (isDashing) return;
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
        if (isDashing || playerTarget == null) return;
        timeSinceLastDash += Time.deltaTime;
        timeSinceLastSpit += Time.deltaTime;

        if (timeSinceLastDash >= meleeDashInterval)
        {
            ForceMeleeReposition();
            timeSinceLastDash = 0f;
            timeUntilMeleeDirectionChange = 0f;
            return;
        }

        if (timeSinceLastDash >= meleeDashInterval - dashApproachDuration)
        {
            navMeshAgent.speed = pursuitSpeed;
            navMeshAgent.stoppingDistance = enemyAttack.attackRange * 1.5f;
            navMeshAgent.SetDestination(playerTarget.position);
            return;
        }

        navMeshAgent.speed = meleeCirclingSpeed;
        navMeshAgent.stoppingDistance = 0;

        if (spitAttackComponent != null && timeSinceLastSpit >= spitCooldown && Random.value < spitChance)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= spitAttackComponent.attackRange && CanSeePlayer(distanceToPlayer))
            {
                SetState(AIState.TelegraphingSpit);
                return;
            }
        }

        timeUntilMeleeDirectionChange -= Time.deltaTime;

        if (timeUntilMeleeDirectionChange <= 0f || (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f))
        {
            timeUntilMeleeDirectionChange = Random.Range(meleeDirectionChangeInterval * 0.6f, meleeDirectionChangeInterval * 1.1f);
            circlingDirection = (Random.value > 0.5f) ? 1 : -1;

            Vector3 directionToPlayer = (transform.position - playerTarget.position).normalized;
            Vector3 idealPosition = playerTarget.position + directionToPlayer * meleeCirclingDistance;
            Vector3 perpendicularDirection = Vector3.Cross(directionToPlayer, Vector3.up).normalized * circlingDirection;
            Vector3 finalDestination = idealPosition + perpendicularDirection * 5f;

            if (NavMesh.SamplePosition(finalDestination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }
        }
    }

    private void TelegraphSpit()
    {
        if (playerTarget == null) { SetState(AIState.Patrolling); return; }

        FaceTarget(playerTarget.position);

        if (timeInCurrentState >= spitTelegraphDuration)
        {
            SetState(AIState.Attacking);
        }
    }

    private void Flee()
    {
        if (playerTarget == null) { SetState(AIState.Patrolling); return; }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (!isAttacking && enemyAttack.CanAttack && distanceToPlayer <= enemyAttack.attackRange && CanSeePlayer(distanceToPlayer))
        {
            SetState(AIState.Telegraphing);
            return;
        }

        bool stillAgainstWall = IsAgainstWall();

        if (!stillAgainstWall)
        {
            SetState(AIState.Lurking);
            return;
        }

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
        {
            SetState(AIState.Fleeing);
        }
    }

    private bool IsAgainstWall()
    {
        Vector3[] checkDirections = { -transform.forward, transform.right, -transform.right };
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;

        foreach (Vector3 dir in checkDirections)
        {
            if (Physics.Raycast(rayOrigin, dir, wallAvoidDistance, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    private Transform GetClosestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return null;

        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (Transform potentialTarget in patrolPoints)
        {
            if (potentialTarget == null) continue;

            Vector3 directionToTarget = potentialTarget.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;
            }
        }
        return bestTarget;
    }

    private void Repositioning()
    {
        if (playerTarget == null) { SetState(AIState.Patrolling); return; }

        FaceTarget(playerTarget.position);

        if (timeInCurrentState >= repositionDuration)
        {
            SetState(AIState.Lurking);
            return;
        }

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
        {
            SetState(AIState.Lurking);
            return;
        }
    }
    private void Attack()
    {
        if (playerTarget == null) { SetState(AIState.Patrolling); return; }

        if (isPerformingSpit)
        {
            if (timeInCurrentState < Time.deltaTime * 2)
            {
                FaceTarget(playerTarget.position);
                spitAttackComponent.PerformAttack(playerTarget);
            }

            if (timeInCurrentState >= spitAttackComponent.attackDuration)
            {
                isAttacking = false;
                isPerformingSpit = false;
                SetState(AIState.Lurking);
            }
        }
        else
        {
            if (timeInCurrentState < Time.deltaTime * 2)
            {
                FaceTarget(playerTarget.position);
                enemyAttack.PerformAttack(playerTarget);
            }

            if (timeInCurrentState >= enemyAttack.attackDuration)
            {
                isAttacking = false;
                SetState(AIState.Repositioning);
            }
        }
    }

    private void Telegraph()
    {
        if (playerTarget == null) { SetState(AIState.Patrolling); return; }
        FaceTarget(playerTarget.position);

        if (timeInCurrentState >= telegraphDuration)
        {
            SetState(AIState.Attacking);
        }
    }
    private void ForceMeleeReposition()
    {
        if (playerTarget == null) return;

        float repositionRadius = enemyAttack.attackRange * 0.9f;
        Vector2 randomPoint2D = Random.insideUnitCircle.normalized * repositionRadius;
        Vector3 randomPoint3D = new Vector3(playerTarget.position.x + randomPoint2D.x,
                                            transform.position.y,
                                            playerTarget.position.z + randomPoint2D.y);

        if (NavMesh.SamplePosition(randomPoint3D, out NavMeshHit hit, repositionRadius, NavMesh.AllAreas))
        {
            float distance = Vector3.Distance(transform.position, hit.position);

            if (meleeDashSpeed < 0.1f) meleeDashSpeed = 25f;
            if (distance < 0.5f)
            {
                if (NavMesh.SamplePosition(hit.position, out NavMeshHit warpHit, 1.0f, NavMesh.AllAreas))
                {
                    navMeshAgent.Warp(warpHit.position);
                }
                return;
            }
            float dashDuration = distance / meleeDashSpeed;
            StartCoroutine(DashReposition(hit.position, dashDuration));
        }
    }
    private void LurkRanged()
    {
        if (playerTarget == null) return;
        timeUntilTacticChange -= Time.deltaTime;
        if (timeUntilTacticChange <= 0f)
        {
            timeUntilTacticChange = Random.Range(rangedTacticChangeInterval * 0.8f, rangedTacticChangeInterval * 1.2f);

            if (Random.value > 0.5f)
            {
                currentKiteWeight = kitingWeight;
                currentStrafeWeight = strafeWeight * 0.2f;
            }
            else
            {
                currentKiteWeight = kitingWeight * 0.2f;
                currentStrafeWeight = strafeWeight;
                circlingDirection = (Random.value > 0.5f) ? 1 : -1;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool isPanicked = (distanceToPlayer <= playerPanicDistance);
        Vector3 kitingDirection = (transform.position - playerTarget.position).normalized;
        if (kitingDirection == Vector3.zero) kitingDirection = -transform.forward;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        bool isAgainstWall = Physics.Raycast(rayOrigin, kitingDirection, wallAvoidDistance, obstacleMask);

        if (isPanicked && isAgainstWall)
        {
            SetState(AIState.Fleeing);
            return;
        }

        if (navMeshAgent.hasPath && navMeshAgent.velocity.sqrMagnitude < 0.1f * 0.1f)
        {
            timeStuck += Time.deltaTime;
        }
        else
        {
            timeStuck = 0f;
        }
        bool isStuck = timeStuck > 0.5f;

        timeUntilRangedMoveTick -= Time.deltaTime;
        if (timeUntilRangedMoveTick <= 0f || isStuck)
        {
            timeUntilRangedMoveTick = Random.Range(repositionTickRate * 0.8f, repositionTickRate * 1.2f);
            if (isStuck)
            {
                timeStuck = 0f;
                circlingDirection *= -1;
            }

            Vector3 directionToPlayerNormalized = (playerTarget.position - transform.position).normalized;

            Vector3 kitingVector = Vector3.zero;
            float distanceError = distanceToPlayer - desiredRangedDistance;
            if (Mathf.Abs(distanceError) > distanceDeadzone)
            {
                kitingVector = directionToPlayerNormalized * distanceError;
            }

            Vector3 strafeVector = Vector3.Cross(directionToPlayerNormalized, transform.up).normalized * circlingDirection;

            if (Physics.Raycast(rayOrigin, strafeVector, 2.0f, obstacleMask))
            {
                strafeVector = Vector3.zero;
                circlingDirection *= -1;
                timeUntilTacticChange = 0f;
            }

            Vector3 combinedDirection = (kitingVector * currentKiteWeight + strafeVector * currentStrafeWeight).normalized;

            if (combinedDirection.sqrMagnitude > 0.1f)
            {
                float lookAheadDistance = 4f;
                Vector3 targetPosition = transform.position + combinedDirection * lookAheadDistance;

                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, lookAheadDistance, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(hit.position);
                }
                else
                {
                    Vector3 kitingTarget = transform.position + (kitingVector.normalized * lookAheadDistance);
                    if (kitingVector.sqrMagnitude > 0.1f && NavMesh.SamplePosition(kitingTarget, out NavMeshHit kitingHit, lookAheadDistance, NavMesh.AllAreas))
                    {
                        navMeshAgent.SetDestination(kitingHit.position);
                    }
                }
            }
            else
            {
                navMeshAgent.ResetPath();
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
        float speed = (currentState == AIState.Attacking || currentState == AIState.Idle) ? 0f : navMeshAgent.velocity.magnitude;
        Vector3 localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
        float velocityX = localVelocity.x;
        float velocityZ = localVelocity.z;
        animator.SetFloat(animatorSpeedParamHash, speed);
        animator.SetFloat(animatorVelocityXHash, velocityX);
        animator.SetFloat(animatorVelocityZHash, velocityZ);
        if (stats != null)
        {
            float healthPercent = stats.CurrentHealth / stats.maxHealth;
            animator.SetFloat(animatorHealthHash, healthPercent);
        }
    }
    private bool CanSeePlayer(float distanceToPlayer)
    {
        if (playerTarget == null) return false;
        if (distanceToPlayer > maxDetectionRadius) return false;

        Vector3 directionToPlayerHorizontal = (playerTarget.position - transform.position);
        directionToPlayerHorizontal.y = 0;
        if (directionToPlayerHorizontal.sqrMagnitude > 0.01f)
        {
            if (Vector3.Angle(transform.forward, directionToPlayerHorizontal.normalized) > viewAngle / 2)
                return false;
        }

        float eyeHeight = 1.5f;
        Vector3 rayOrigin = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = playerTarget.position + Vector3.up * eyeHeight;

        Vector3 directionToTarget = (targetPosition - rayOrigin).normalized;
        float distanceToTarget = Vector3.Distance(rayOrigin, targetPosition);

        if (Physics.Raycast(rayOrigin, directionToTarget, distanceToTarget, obstacleMask))
        {
            Debug.DrawRay(rayOrigin, directionToTarget * distanceToTarget, Color.red);
            return false;
        }

        Debug.DrawRay(rayOrigin, directionToTarget * distanceToTarget, Color.green);
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