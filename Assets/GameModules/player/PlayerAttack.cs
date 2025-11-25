using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PlayerAttack : MonoBehaviour
{
    [Header("Configurações do Ataque")]
    public Transform attackPoint;
    public int attackDamage = 20; // Dano base (será modificado pelo Buff)
    
    // Novo: Prefab do Slash (Projétil)
    public GameObject slashEffectPrefab; 

    [Header("Sistema de Combo")]
    public float comboBufferDuration = 1.0f;

    public bool isAttacking { get; private set; }

    // --- Componentes ---
    private Animator animator;
    private Rigidbody rb;
    private PlayerInputActions controls;

    // --- Variáveis Internas ---
    private int comboCounter = 0;
    private bool comboInputReceived = false;
    private Coroutine comboWindowCoroutine;
    
    // Variáveis de Buff
    private int defaultDamage;
    private Coroutine damageBoostCoroutine;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        controls = new PlayerInputActions();
        controls.Player.Attack.performed += ctx => TryAttack();
        defaultDamage = attackDamage;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    // Mantida a lógica de Buff de Dano
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
        comboInputReceived = false;
        comboCounter = step;

        animator.SetInteger("ComboStep", comboCounter);
        animator.SetTrigger("Attack");

        // --- Instanciar o Projétil de Slash ---
        if (slashEffectPrefab != null && attackPoint != null)
        {
            GameObject slashObj = Instantiate(slashEffectPrefab, attackPoint.position, transform.rotation);
            
            // Passa o dano atual (que pode estar buffado) para o projétil
            SlashProjectile proj = slashObj.GetComponent<SlashProjectile>();
            if (proj != null)
            {
                proj.damage = attackDamage; 
            }
        }
        else
        {
            Debug.LogWarning("Faltando Slash Prefab ou Attack Point!");
        }

        // REMOVIDO: O AttackDashCoroutine antigo. 
        // O movimento agora é controlado pelo PlayerMovement (ficando lento)

        // Janela de combo
        if (comboWindowCoroutine != null) StopCoroutine(comboWindowCoroutine);
        comboWindowCoroutine = StartCoroutine(ComboWindowCoroutine());
    }

    private IEnumerator ComboWindowCoroutine()
    {
        // Debug.Log("Janela de combo ABERTA.");
        yield return new WaitForSeconds(comboBufferDuration);
        // Debug.Log("Janela de combo FECHADA.");
        comboWindowCoroutine = null;
    }

    // --- FUNÇÃO DE EVENTO DE ANIMAÇÃO ---
    public void FinishAttackAnimation()
    {
        if (comboWindowCoroutine != null)
        {
            StopCoroutine(comboWindowCoroutine);
            comboWindowCoroutine = null;
        }

        if (comboInputReceived)
        {
            if (comboCounter == 4) // Ajuste conforme seu combo máximo
            {
                ResetCombo();
                return;
            }
            PerformAttack(comboCounter + 1);
        }
        else
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        isAttacking = false;
        comboInputReceived = false;
        comboCounter = 0;
        animator.SetInteger("ComboStep", 0);
        // Garante que a gravidade está ativa (caso alguma lógica antiga tenha mexido)
        rb.useGravity = true; 
    }

    public void CancelAttack()
    {
        if (comboWindowCoroutine != null) StopCoroutine(comboWindowCoroutine);

        comboWindowCoroutine = null;
        ResetCombo();
        animator.Play("Idle");
        rb.useGravity = true;
    }

    public void SetAnimationSpeed(float speed)
    {
        if (animator != null) animator.speed = speed;
    }
    
    // OBS: As funções StartAttackHitbox/FinishAttackHitbox foram removidas
    // pois o hit agora é calculado pelo projétil.
}