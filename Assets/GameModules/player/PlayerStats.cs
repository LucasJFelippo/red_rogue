using UnityEngine;
using System; 

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDeath;

    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }
    public void TakeDamage(int damage)
    {
        if (isDead) return; // Se já estiver morto, não faz nada

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player tomou " + damage + " de dano. Vida restante: " + currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        int healAmount = Mathf.RoundToInt(amount);

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player curou " + healAmount + ". Vida atual: " + currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player morreu!");

        // Dispara o evento de morte
        OnPlayerDeath?.Invoke();

        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;

        GetComponent<Animator>().SetTrigger("Death");
    }
}