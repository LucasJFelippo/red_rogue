using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAttack : MonoBehaviour
{
    public enum AttackStyle { Melee, Ranged }

    [Header("Style")]
    public AttackStyle attackStyle = AttackStyle.Melee;

    [Header("Common Settings")]
    public float attackRange = 5f;
    public float attackDuration = 1f;
    [Header("Animation Settings")]
    [Tooltip("Nome exato do ESTADO da animação no Animator, não o Gatilho.")]
    public string attackStateName = "Attack"; 
    [Tooltip("Tempo para esperar antes de spawnar o projétil/hitbox.")]
    public float attackImpactDelay = 0.3f;

    [Header("Ranged Attack Settings")]
    public GameObject projectilePrefab;

    [Tooltip("Lista de spawn points. O inimigo alternará entre eles a cada ataque.")]
    public Transform[] projectileSpawnPoints;
    private Animator animator;
    private EnemyStats stats;

    private int animatorAttackTriggerHash;
    private float timeSinceLastAttack = 0f;
    private int currentSpawnPointIndex = 0;
    public bool CanAttack => timeSinceLastAttack >= stats.attackCooldown;

    void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();
    }

    void Start()
    {
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
        timeSinceLastAttack = 0f;
        animator.CrossFadeInFixedTime(attackStateName, 0.1f);
        StartCoroutine(AttackRoutine(target));
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        yield return new WaitForSeconds(attackImpactDelay);

        if (attackStyle == AttackStyle.Ranged)
        {
            HandleRangedAttack(target);
        }
        else
        {
        }
    }

    private void HandleRangedAttack(Transform target)
    {
        if (projectilePrefab == null) return;
        if (projectileSpawnPoints == null || projectileSpawnPoints.Length == 0) return;

        Transform currentPoint = projectileSpawnPoints[currentSpawnPointIndex];

        if (currentPoint != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, currentPoint.position, currentPoint.rotation);
            if (target != null)
            {
                Vector3 alvo = target.position;
                alvo.y = transform.position.y;
                projectile.transform.LookAt(alvo);
            }
        }
        currentSpawnPointIndex = (currentSpawnPointIndex + 1) % projectileSpawnPoints.Length;
    }
}
