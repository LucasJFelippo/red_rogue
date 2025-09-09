using UnityEngine;
using System; // Necessário para usar 'Action'

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    // Evento para notificar a UI ou outros sistemas quando a vida muda
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

        // Dispara o evento para notificar a mudança de vida
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player morreu!");

        // Dispara o evento de morte
        OnPlayerDeath?.Invoke();

        // --- LÓGICA DE MORTE DO PLAYER ---
        // Aqui você pode desativar o controle do jogador, tocar uma animação de morte,
        // e depois de alguns segundos, recarregar a cena ou mostrar uma tela de "Game Over".

        // Exemplo: Desativar componentes de movimento e ataque
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;

        // Toca a animação de morte (se você tiver uma)
        GetComponent<Animator>().SetTrigger("Death");

        // Exemplo de como recarregar a cena depois de 5 segundos
        // Invoke("ReloadScene", 5f);
    }

    // Função exemplo para recarregar a cena
    // void ReloadScene()
    // {
    //     UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    // }
}