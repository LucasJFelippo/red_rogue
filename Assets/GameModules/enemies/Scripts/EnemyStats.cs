using UnityEngine;
using System;

public class EnemyStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    [Tooltip("The current health of the enemy.")]
    [SerializeField] private float currentHealth;

    [Header("Mana / Energy")]
    public float maxMana = 50f;
    [SerializeField] private float currentMana;

    [Header("Attack Stats")]
    public float attackDamage = 10f;
    public float attackCooldown = 2f;

    // --- Events ---
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    // --- Properties ---
    public float CurrentHealth => currentHealth;
    public float CurrentMana => currentMana;

    // --- State ---
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
    }

    void Update()
    {
        if (!isDead && currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"{gameObject.name} took {damageAmount} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool UseMana(float manaCost)
    {
        if (currentMana >= manaCost)
        {
            currentMana -= manaCost;
            return true;
        }
        return false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} has died and will stop.");
        OnDeath?.Invoke();

        var aiComponent = GetComponent<EnemyNavMeshAI>();
        if (aiComponent != null)
        {
            aiComponent.enabled = false;
        }

        var agentComponent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agentComponent != null)
        {
            if (agentComponent.enabled && agentComponent.isOnNavMesh)
            {
                agentComponent.velocity = Vector3.zero;
                agentComponent.updateRotation = false;
                agentComponent.isStopped = true;
                agentComponent.ResetPath();
            }
            agentComponent.enabled = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        var colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.enabled = false;
        }

        var animatorComponent = GetComponent<Animator>();
        if (animatorComponent != null)
        {
            animatorComponent.applyRootMotion = false;
            animatorComponent.SetFloat("MovementSpeed", 0);
            animatorComponent.SetTrigger("Death");
        }

        Destroy(gameObject, 5f);
    }
}
