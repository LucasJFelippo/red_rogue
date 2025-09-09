using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    public bool isAttacking { get; private set; } // Trava de estado de ataque

    [Header("Configurações do Ataque")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    private Animator animator;
    private PlayerInputActions controls;
    private bool isHitboxActive = false;
    private List<Collider> enemiesHitThisAttack;

    void Awake()
    {
        animator = GetComponent<Animator>();
        controls = new PlayerInputActions();
        controls.Player.Attack.performed += ctx => Attack();
        enemiesHitThisAttack = new List<Collider>();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        if (isHitboxActive) { PerformHitCheck(); }
    }

    private void Attack()
    {
        if (isAttacking) return; // Se já estiver atacando, não faz nada

        isAttacking = true; // ATIVA A TRAVA GERAL
        enemiesHitThisAttack.Clear();
        animator.SetTrigger("Attack1");
    }

    private void PerformHitCheck()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider enemy in hitEnemies)
        {
            if (!enemiesHitThisAttack.Contains(enemy))
            {
                Debug.Log("Acertamos " + enemy.name);
                enemiesHitThisAttack.Add(enemy);
            }
        }
    }

    public void StartAttackHitbox() { isHitboxActive = true; }
    public void FinishAttackHitbox()
    {
        isHitboxActive = false;
        isAttacking = false;
    }

    // Chamada pelo último frame da animação para liberar o controle

    void OnDrawGizmos()
    {
        if (!isHitboxActive) return;
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}