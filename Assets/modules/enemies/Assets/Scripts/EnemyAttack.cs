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

    private Animator animator;
    private int animatorAttackTriggerHash;
    private float timeSinceLastAttack = 0f;

    public bool CanAttack => timeSinceLastAttack >= attackCooldown;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        animatorAttackTriggerHash = Animator.StringToHash(animatorAttackTrigger);
        timeSinceLastAttack = attackCooldown;
    }

    void Update()
    {
        if (timeSinceLastAttack < attackCooldown)
        {
            timeSinceLastAttack += Time.deltaTime;
        }
    }

    public void PerformAttack()
    {
        if (!CanAttack) return;
        animator.SetTrigger(animatorAttackTriggerHash);
        timeSinceLastAttack = 0f;
    }
}
