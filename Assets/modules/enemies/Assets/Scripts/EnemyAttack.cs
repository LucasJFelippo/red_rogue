using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("The distance from which the enemy can launch an attack.")]
    public float attackRange = 5f;
    [Tooltip("Time between enemy attacks (in seconds).")]
    public float attackCooldown = 2f;
    [Tooltip("Name of the trigger parameter in the Animator for attacking.")]
    public string animatorAttackTrigger = "Attack";

    // --- Private Fields ---
    private Animator animator;
    private int animatorAttackTriggerHash;
    private float timeSinceLastAttack = 0f;

    /// <summary>
    /// Public property to check if the cooldown has passed and the enemy can attack.
    /// </summary>
    public bool CanAttack => timeSinceLastAttack >= attackCooldown;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Cache the animator parameter hash for performance
        animatorAttackTriggerHash = Animator.StringToHash(animatorAttackTrigger);
        // Start cooldown timer high so the enemy can attack immediately if needed
        timeSinceLastAttack = attackCooldown;
    }

    void Update()
    {
        // Increment the cooldown timer
        if (timeSinceLastAttack < attackCooldown)
        {
            timeSinceLastAttack += Time.deltaTime;
        }
    }

    /// <summary>
    /// This is the public method that the main AI script will call to initiate an attack.
    /// </summary>
    public void PerformAttack()
    {
        if (!CanAttack) return;

        // Trigger the animation and reset the cooldown timer.
        animator.SetTrigger(animatorAttackTriggerHash);
        timeSinceLastAttack = 0f;
    }
}
