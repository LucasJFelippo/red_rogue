using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerPursuit : MonoBehaviour
{
    [Header("Pursuit Settings")]
    public float pursuitSpeed = 4f;
    public float rotationSpeed = 360f;
    public float stoppingDistance = 2f;

    [Header("Animation Settings")]
    public string animatorSpeedParameter = "MovementSpeed";

    private Transform playerTarget;
    private Animator animator;
    private Vector3 lastPosition;
    private int animatorSpeedParamHash;


    void Awake()
    {
        animator = GetComponent<Animator>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    // OnEnable is called when the script becomes enabled.
    // It's a good place to reset values for when the state begins.
    void OnEnable()
    {
        if (animator == null)
        {
            Debug.LogError("PlayerPursuit: Animator component not found!", this);
            enabled = false;
            return;
        }
        animatorSpeedParamHash = Animator.StringToHash(animatorSpeedParameter);
        lastPosition = transform.position;
    }

    void Update()
    {
        if (playerTarget == null || animator == null)
        {
            if (animator != null) animator.SetFloat(animatorSpeedParamHash, 0f);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        Vector3 directionToPlayer = (playerTarget.position - transform.position);
        directionToPlayer.y = 0;
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (distanceToPlayer > stoppingDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, pursuitSpeed * Time.deltaTime);
        }

        float currentFrameSpeed = 0f;
        if (Time.deltaTime > 0)
        {
            currentFrameSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        }
        lastPosition = transform.position;
        animator.SetFloat(animatorSpeedParamHash, currentFrameSpeed);

        // FOR DEBUGGING: Uncomment the line below to see the speed value in the console.
        // Debug.Log($"Pursuit Speed: {currentFrameSpeed}");
    }
}
