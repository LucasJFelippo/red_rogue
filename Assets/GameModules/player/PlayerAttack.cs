using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PlayerAttack : MonoBehaviour
{
    [Header("Configurações do Ataque")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public int attackDamage = 20;
    public LayerMask enemyLayers;
    public LayerMask wallLayer;

    [Header("Avanço do Ataque")]
    public float attackDashDistance = 1.5f;
    public float attackDashDuration = 0.15f;

    [Header("Sistema de Combo (Sua Lógica)")]
    public float comboBufferDuration = 1.0f;

    public bool isAttacking { get; private set; }

    // --- Componentes ---
    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerInputActions controls;

    // --- Variáveis Internas ---
    private int comboCounter = 0;
    private bool comboInputReceived = false;
    private Coroutine attackDashCoroutine;
    private Coroutine comboWindowCoroutine;
    private List<Collider> enemiesHitThisAttack;
    private bool isHitboxActive = false;
    private int defaultDamage;
    private Coroutine damageBoostCoroutine;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        controls = new PlayerInputActions();
        controls.Player.Attack.performed += ctx => TryAttack();
        enemiesHitThisAttack = new List<Collider>();
        defaultDamage = attackDamage;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        // Não precisamos mais do timer no Update
        if (isHitboxActive) { PerformHitCheck(); }
    }

    public void ApplyDamageBoost(float multiplier, float duration)
    {
        if (damageBoostCoroutine != null)
        {
            StopCoroutine(damageBoostCoroutine);
        }
        damageBoostCoroutine = StartCoroutine(DamageBoostRoutine(multiplier, duration));
    }

    private IEnumerator DamageBoostRoutine(float multiplier, float duration)
    {
        attackDamage = Mathf.RoundToInt(defaultDamage * multiplier);

        yield return new WaitForSeconds(duration);

        attackDamage = defaultDamage;
        damageBoostCoroutine = null;
    }
    private void TryAttack()
    {
        PlayerMovement pm = GetComponent<PlayerMovement>();

        if (!isAttacking && (pm == null || !pm.isDashing))
        {
            PerformAttack(1);
        }
        else if (isAttacking)
        {
            if (comboWindowCoroutine != null)
            {
                Debug.Log("Input de combo registrado!");
                comboInputReceived = true;
            }
        }
    }

    private void PerformAttack(int step)
    {
        isAttacking = true;
        comboInputReceived = false; // Limpa o registro para este novo golpe
        comboCounter = step;

        animator.SetInteger("ComboStep", comboCounter);
        animator.SetTrigger("Attack");

        // Inicia o dash de ataque
        if (attackDashCoroutine != null) StopCoroutine(attackDashCoroutine);
        attackDashCoroutine = StartCoroutine(AttackDashCoroutine(attackDashDistance, attackDashDuration));

        // Inicia a janela de buffer de 1 segundo
        if (comboWindowCoroutine != null) StopCoroutine(comboWindowCoroutine);
        comboWindowCoroutine = StartCoroutine(ComboWindowCoroutine());
    }

    // A corrotina que define o seu timer de 1 segundo
    private IEnumerator ComboWindowCoroutine()
    {
        Debug.Log("Janela de combo ABERTA por " + comboBufferDuration + "s.");
        yield return new WaitForSeconds(comboBufferDuration);

        Debug.Log("Janela de combo FECHADA.");
        comboWindowCoroutine = null;
    }

    // --- FUNÇÃO DE EVENTO DE ANIMAÇÃO
    public void FinishAttackAnimation()
    {
        // Se a janela de 1s ainda estava aberta, nós a fechamos
        if (comboWindowCoroutine != null)
        {
            StopCoroutine(comboWindowCoroutine);
            comboWindowCoroutine = null;
        }

        // Verifica se o clique foi registrado DENTRO daquela janela
        if (comboInputReceived)
        {
            if (comboCounter == 4)
            {
                Debug.Log("FIM combo!");
                comboCounter = 0;
                animator.SetInteger("ComboStep", 0);
                isAttacking = false;
                return;
            }
            Debug.Log("Animação terminou, indo para o próximo combo!");
            PerformAttack(comboCounter + 1);
        }
        else
        {
            Debug.Log("Animação terminou, resetando o combo.");
            isAttacking = false;
            comboCounter = 0;
            animator.SetInteger("ComboStep", 0);
        }
    }

    // Função pública para o Dash poder cancelar o ataque
    public void CancelAttack()
    {
        if (attackDashCoroutine != null) StopCoroutine(attackDashCoroutine);
        if (comboWindowCoroutine != null) StopCoroutine(comboWindowCoroutine);

        comboWindowCoroutine = null;
        isAttacking = false;
        comboInputReceived = false;
        comboCounter = 0;
        animator.SetInteger("ComboStep", 0);
        animator.Play("Idle");
        rb.useGravity = true;
    }

    private IEnumerator AttackDashCoroutine(float distance, float duration)
    {
        rb.useGravity = false;
        float elapsedTime = 0f;
        float speed = distance / duration;
        Vector3 direction = transform.forward;

        while (elapsedTime < duration)
        {
            Vector3 movementStep = direction * speed * Time.fixedDeltaTime;
            Vector3 newPos = rb.position + movementStep;
            if (Physics.CapsuleCast(
                rb.position + capsule.center + Vector3.up * -capsule.height * 0.5f,
                rb.position + capsule.center + Vector3.up * capsule.height * 0.5f,
                capsule.radius,
                direction,
                out RaycastHit hit,
                movementStep.magnitude,
                wallLayer))
            {
                rb.MovePosition(hit.point - direction * capsule.radius);
                break;
            }
            rb.MovePosition(newPos);
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.useGravity = true;
    }

    private void PerformHitCheck()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider enemyCollider in hitEnemies)
        {
            if (!enemiesHitThisAttack.Contains(enemyCollider))
            {
                EnemyStats enemyStats = enemyCollider.GetComponent<EnemyStats>();
                if (enemyStats != null)
                {
                    enemyStats.TakeDamage(attackDamage);
                }
                Debug.Log("Acertou " + enemyCollider.name);
                enemiesHitThisAttack.Add(enemyCollider);
            }
        }
    }

    public void StartAttackHitbox()
    {
        isHitboxActive = true;
        enemiesHitThisAttack.Clear();
    }

    public void FinishAttackHitbox()
    {
        isHitboxActive = false;
    }

    public void SetAnimationSpeed(float speed)
    {
        if (animator != null) animator.speed = speed;
    }

    void OnDrawGizmos()
    {
        if (attackPoint == null) return;
        if (isHitboxActive) Gizmos.color = Color.red;
        else Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}