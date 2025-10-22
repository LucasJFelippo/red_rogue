using UnityEngine;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAttack : MonoBehaviour
{
    public enum AttackStyle { Melee, Ranged }

    [Header("Style")]
    public AttackStyle attackStyle = AttackStyle.Melee;

    [Header("Common Settings")]
    public float attackRange = 5f;
    [Tooltip("How long the attack animation takes.")]
    public float attackDuration = 1f;
    public string animatorAttackTrigger = "Attack";

    [Header("Ranged Attack Settings")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;

    private Animator animator;
    private EnemyStats stats;

    private int animatorAttackTriggerHash;
    private float timeSinceLastAttack = 0f;


    public bool CanAttack => timeSinceLastAttack >= stats.attackCooldown;

    void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();
    }

    void Start()
    {
        animatorAttackTriggerHash = Animator.StringToHash(animatorAttackTrigger);

        if (stats != null)
        {
            timeSinceLastAttack = stats.attackCooldown;
        }
    }

    void Update()
    {
        if (stats != null && timeSinceLastAttack < stats.attackCooldown)
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

            Vector3 alvo = target.position;
            alvo.y = transform.position.y;
            projectile.transform.LookAt(alvo);
        }
        if (attackStyle == AttackStyle.Melee)
        {
            if (projectilePrefab == null || projectileSpawnPoint == null)
            {
                Debug.LogError("Spit attack failed: Projectile Prefab or Spawn Point is not set.", this);
                return;
            }

            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);

            Vector3 alvo = target.position;
            alvo.y = transform.position.y;
            projectile.transform.LookAt(alvo);
        }

    }
}
