using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAttack : MonoBehaviour
{
    public enum AttackStyle { Melee, Ranged }

    [Header("Style")]
    public AttackStyle attackStyle = AttackStyle.Melee;

    [Header("Common Settings")]
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    [Tooltip("How long the attack animation takes. The AI will be locked in an 'Attacking' state for this duration.")]
    public float attackDuration = 1f;
    public string animatorAttackTrigger = "Attack";

    [Header("Ranged Attack Settings")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;

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

    public void PerformAttack(Transform target)
    {
        if (!CanAttack) return;

        animator.SetTrigger(animatorAttackTriggerHash);
        timeSinceLastAttack = 0f;

        if (attackStyle == AttackStyle.Ranged)
        {
            if (projectilePrefab == null || projectileSpawnPoint == null)
            {
                Debug.LogError("Ranged attack failed: Projectile Prefab or Spawn Point is not set.", this);
                return;
            }

            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            projectile.transform.LookAt(target.position);
        }
    }
}
