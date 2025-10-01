using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    // --- Variáveis Antigas ---
    public bool isAttacking { get; private set; }
    [Header("Configurações do Ataque")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    // --- Variáveis para o Avanço Controlado ---
    [Header("Avanço do Ataque")]
    public float attackDashDistance = 1.5f; // A distância que o player avança
    public float attackDashDuration = 0.15f; // O tempo que leva para avançar

    // --- Variáveis para Combo e Avanço ---
    [Header("Sistema de Combo")]
    public int attackDamage = 20;
    public float attackDashForce = 5f; // Força do "pulinho" pra frente ao atacar
    private int comboCounter = 0; // Controla qual ataque estamos no combo (0, 1, 2...)
    private bool canReceiveComboInput = false; // Janela de tempo para registrar o próximo clique
    private bool comboInputReceived = false; // Registra se o jogador clicou para o próximo combo

    // --- Componentes ---
    private Animator animator;
    private Rigidbody rb; // Precisamos do Rigidbody para o avanço
    private PlayerInputActions controls;
    private bool isHitboxActive = false;
    private List<Collider> enemiesHitThisAttack;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>(); // Pegamos a referência do Rigidbody
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
        // Se a janela de combo estiver aberta, registra o input para o próximo ataque
        if (canReceiveComboInput)
        {
            comboInputReceived = true;
            return;
        }

        if (isAttacking) return; // Se já estiver atacando, não faz nada

        isAttacking = true; // ATIVA A TRAVA GERAL
        comboCounter = 1; // Inicia o primeiro ataque do combo
        animator.SetInteger("ComboStep", comboCounter);

        animator.SetTrigger("Attack1");

        // Aplica o dash de ataque
        StartCoroutine(AttackDashCoroutine(attackDashDistance, attackDashDuration));
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
                    Debug.Log("Acertamos " + enemyCollider.name + "e causamos" + attackDamage + " de dano.");
                }
                enemiesHitThisAttack.Add(enemyCollider);
            }
        }
    }

    // --- CORROTINA PARA O AVANÇO DO ATAQUE ---
    private IEnumerator AttackDashCoroutine(float distance, float duration)
    {
        Vector3 startPosition = rb.position;
        Vector3 endPosition = startPosition + transform.forward * distance;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            rb.MovePosition(Vector3.Lerp(startPosition, endPosition, progress));
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(endPosition);
    }

    // --- Funções chamadas por Animation Events ---

    // Evento no INÍCIO da animação de ataque
    public void AttackStart()
    {
        isAttacking = true;
        enemiesHitThisAttack.Clear();
    }

    // Evento para ABRIR a janela de input para o próximo combo
    public void OpenComboWindow()
    {
        canReceiveComboInput = true;
        isAttacking = true;
    }

    // Evento para FECHAR a janela de input e decidir se continua o combo
    public void CloseComboWindowAndCheckNext()
    {
        canReceiveComboInput = false;

        if (comboInputReceived) // Se o jogador apertou o botão de novo...
        {
            comboCounter++;
            Debug.Log(comboCounter);
            animator.SetInteger("ComboStep", comboCounter); // Prepara o próximo ataque
            comboInputReceived = false;

            // Aplica o dash de ataque
            StartCoroutine(AttackDashCoroutine(attackDashDistance, attackDashDuration));
        }
        else // Se não, encerra o combo
        {
            FinishAttackAnimation();
        }
    }

    // Evento chamado no FIM da última animação de ataque do combo
    public void FinishAttackAnimation()
    {
        isAttacking = false;
        comboCounter = 0;
        animator.SetInteger("ComboStep", comboCounter); // Reseta o Animator
    }

    // Eventos para controlar a hitbox
    public void StartAttackHitbox() { isHitboxActive = true; }
    public void FinishAttackHitbox() { isHitboxActive = false; isAttacking = false; }
    void OnDrawGizmos()
    {
        if (!isHitboxActive) return;
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}