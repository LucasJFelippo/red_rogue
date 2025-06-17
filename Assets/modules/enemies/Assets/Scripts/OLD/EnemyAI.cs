using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))] // Add Animator requirement
[RequireComponent(typeof(PatternMovement))]
[RequireComponent(typeof(PlayerPursuit))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Patrolling, Pursuing }

    [Header("State Machine")]
    [SerializeField] private AIState currentState = AIState.Patrolling;

    [Header("Detection Settings")]
    public string playerTag = "Player";
    public float viewRadius = 15f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask obstacleMask;
    public float checkInterval = 0.2f;

    // --- Component & Target References ---
    private Transform playerTarget;
    private Animator animator; // NEW: Reference to the Animator
    private PatternMovement patternMovement;
    private PlayerPursuit playerPursuit;

    void Awake()
    {
        animator = GetComponent<Animator>(); // NEW: Get the Animator component
        patternMovement = GetComponent<PatternMovement>();
        playerPursuit = GetComponent<PlayerPursuit>();

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogError($"EnemyAI: Cannot find GameObject with tag '{playerTag}'. AI will not pursue.", this);
        }
    }

    void Start()
    {
        SwitchState(AIState.Patrolling);
        StartCoroutine(CheckForPlayerRoutine());
    }

    private void SwitchState(AIState newState)
    {
        if (currentState == newState) return; // Don't switch to the same state

        currentState = newState;

        // Centralized logic to enable/disable scripts based on the new state
        switch (currentState)
        {
            case AIState.Patrolling:
                playerPursuit.enabled = false;
                patternMovement.enabled = true;
                break;
            case AIState.Pursuing:
                patternMovement.enabled = false;
                playerPursuit.enabled = true;
                break;
        }
    }

    IEnumerator CheckForPlayerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        if (playerTarget == null) return;

        bool canSeePlayer = CanSeePlayer();

        if (currentState == AIState.Patrolling && canSeePlayer)
        {
            SwitchState(AIState.Pursuing);
        }
        else if (currentState == AIState.Pursuing && !canSeePlayer)
        {
            SwitchState(AIState.Patrolling);
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTarget == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > viewRadius)
        {
            return false;
        }

        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > viewAngle / 2)
        {
            return false;
        }

        float distanceToPlayerForRaycast = distanceToPlayer;
        if (Physics.Raycast(transform.position, directionToPlayer, distanceToPlayerForRaycast, obstacleMask))
        {
            return false;
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 viewAngleA = DirectionFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirectionFromAngle(viewAngle / 2, false);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        if (playerTarget != null && CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }

    public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
