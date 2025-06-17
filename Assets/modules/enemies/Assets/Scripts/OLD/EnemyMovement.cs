using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PatternMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3[] waypoints;
    public float speed = 2f;
    public float rotationSpeed = 360f;
    public bool lockYPosition = true;

    [Header("Animation Settings")]
    public string animatorSpeedParameter = "MovementSpeed";

    private int currentWaypointIndex = 0;
    private Vector3 initialPosition;
    private float fixedYPosition;
    private Animator animator;
    private Vector3 lastPosition;
    private int animatorSpeedParamHash;

    void OnEnable()
    {
        initialPosition = transform.position;
        fixedYPosition = transform.position.y;
        lastPosition = transform.position;

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            enabled = false;
            return;
        }
        animatorSpeedParamHash = Animator.StringToHash(animatorSpeedParameter);

        if (waypoints == null || waypoints.Length == 0)
        {
            if (animator != null)
            {
                animator.SetFloat(animatorSpeedParamHash, 0f);
            }
            enabled = false;
            return;
        }

        if (waypoints.Length > 0)
        {
            OrientTowardsFirstWaypoint();
        }
    }

    void OrientTowardsFirstWaypoint()
    {
        Vector3 targetOffset = waypoints[currentWaypointIndex];
        Vector3 targetPosition;

        if (lockYPosition)
        {
            targetPosition = new Vector3(
                initialPosition.x + targetOffset.x,
                fixedYPosition + targetOffset.y,
                initialPosition.z + targetOffset.z
            );
        }
        else
        {
            targetPosition = initialPosition + targetOffset;
        }

        Vector3 direction = targetPosition - transform.position;
        if (direction != Vector3.zero)
        {
            if (lockYPosition)
            {
                direction.y = 0;
            }
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }
    }

    void Update()
    {
        if (waypoints.Length == 0 || animator == null)
        {
            if (animator != null) animator.SetFloat(animatorSpeedParamHash, 0f);
            return;
        }

        Vector3 targetOffset = waypoints[currentWaypointIndex];
        Vector3 targetPosition;

        if (lockYPosition)
        {
            targetPosition = new Vector3(
                initialPosition.x + targetOffset.x,
                fixedYPosition + targetOffset.y,
                initialPosition.z + targetOffset.z
            );
        }
        else
        {
            targetPosition = initialPosition + targetOffset;
        }

        Vector3 currentObjectPosition = transform.position;
        Vector3 positionToMoveFrom = currentObjectPosition;

        if (lockYPosition)
        {
            positionToMoveFrom.y = fixedYPosition + targetOffset.y;
        }

        Vector3 directionToTarget = targetPosition - currentObjectPosition;
        if (directionToTarget != Vector3.zero)
        {
            Vector3 lookDirection = directionToTarget;
            if (lockYPosition)
            {
                 lookDirection.y = 0f;
            }
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
                if (rotationSpeed > 0)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = targetRotation;
                }
            }
        }

        transform.position = Vector3.MoveTowards(positionToMoveFrom, targetPosition, speed * Time.deltaTime);
        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = fixedYPosition + targetOffset.y;
            transform.position = pos;
        }

        float currentFrameSpeed = 0f;
        if (Time.deltaTime > 0)
        {
            currentFrameSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        }
        lastPosition = transform.position;

        animator.SetFloat(animatorSpeedParamHash, currentFrameSpeed);

        float distanceToTarget;
        if (lockYPosition)
        {
            Vector2 currentPosXZ = new Vector2(transform.position.x, transform.position.z);
            Vector2 targetPosXZ = new Vector2(targetPosition.x, targetPosition.z);
            distanceToTarget = Vector2.Distance(currentPosXZ, targetPosXZ);
        }
        else
        {
            distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        }

        if (distanceToTarget < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Vector3 previewInitialPosition = Application.isPlaying ? initialPosition : transform.position;
        float previewFixedY = Application.isPlaying ? fixedYPosition : transform.position.y;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Gizmos.color = Color.yellow;
            Vector3 waypointWorldPos;
            if (Application.isPlaying && lockYPosition) { waypointWorldPos = new Vector3(initialPosition.x + waypoints[i].x, fixedYPosition + waypoints[i].y, initialPosition.z + waypoints[i].z); }
            else if (!Application.isPlaying && lockYPosition) { waypointWorldPos = new Vector3(previewInitialPosition.x + waypoints[i].x, previewFixedY + waypoints[i].y, previewInitialPosition.z + waypoints[i].z); }
            else { waypointWorldPos = previewInitialPosition + waypoints[i]; }
            Gizmos.DrawWireSphere(waypointWorldPos, 0.3f);

            Vector3 nextWaypointWorldPos;
            int nextIndex = (i + 1) % waypoints.Length;
            if (Application.isPlaying && lockYPosition) { nextWaypointWorldPos = new Vector3(initialPosition.x + waypoints[nextIndex].x, fixedYPosition + waypoints[nextIndex].y, initialPosition.z + waypoints[nextIndex].z); }
            else if (!Application.isPlaying && lockYPosition) { nextWaypointWorldPos = new Vector3(previewInitialPosition.x + waypoints[nextIndex].x, previewFixedY + waypoints[nextIndex].y, previewInitialPosition.z + waypoints[nextIndex].z); }
            else { nextWaypointWorldPos = previewInitialPosition + waypoints[nextIndex]; }
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(waypointWorldPos, nextWaypointWorldPos);
        }
    }
}
