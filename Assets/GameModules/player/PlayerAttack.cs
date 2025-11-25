using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PlayerAttack : MonoBehaviour
{
    [Header("Configurações do Ataque")]
    public Transform attackPoint;
    public int attackDamage = 20;

    public GameObject slashEffectPrefab;

    [Header("Configurações de Reflexão (Parry)")]
    public float parryRange = 2.0f;
    public LayerMask projectileLayer;

    [Header("Sistema de Combo")]
    public float comboBufferDuration = 1.0f;

    public bool isAttacking { get; private set; }

    private Animator animator;
    private Rigidbody rb;
    private PlayerInputActions controls;

    private int comboCounter = 0;
    private bool comboInputReceived = false;
    private Coroutine comboWindowCoroutine;

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

        CheckForParry();

        if (slashEffectPrefab != null && attackPoint != null)
        {
            GameObject slashObj = Instantiate(slashEffectPrefab, attackPoint.position, transform.rotation);

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

        if (comboWindowCoroutine != null) StopCoroutine(comboWindowCoroutine);
        comboWindowCoroutine = StartCoroutine(ComboWindowCoroutine());
    }
    private void CheckForParry()
    {
        if (attackPoint == null) return;
        Collider[] hitProjectiles = Physics.OverlapSphere(attackPoint.position, parryRange, projectileLayer);

        foreach (Collider projCollider in hitProjectiles)
        {
            Projectile projectile = projCollider.GetComponent<Projectile>();
            if (projectile != null && !projectile.isReflected)
            {
                projectile.Reflect(transform.forward);
                Debug.Log("PARRY!");
            }
        }
    }

    private IEnumerator ComboWindowCoroutine()
    {
        yield return new WaitForSeconds(comboBufferDuration);
        comboWindowCoroutine = null;
    }

    public void FinishAttackAnimation()
    {
        if (comboWindowCoroutine != null)
        {
            StopCoroutine(comboWindowCoroutine);
            comboWindowCoroutine = null;
        }

        if (comboInputReceived)
        {
            if (comboCounter == 4)
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
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, parryRange);
        }
    }
}