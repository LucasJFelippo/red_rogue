using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI Image that will be used as the health bar fill.")]
    public Image healthBarFill;

    [Header("Dependencies")]
    [Tooltip("The EnemyStats component to track.")]
    public EnemyStats enemyStats;

    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("EnemyHealthBar: Main Camera not found. Health bar will not face the camera.", this);
        }

        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(enemyStats.CurrentHealth, enemyStats.maxHealth);
        }
        else
        {
            Debug.LogError("EnemyHealthBar: EnemyStats component not assigned!", this);
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged -= UpdateHealthBar;
        }
    }

    void Update()
    {
        if (enemyStats != null && healthBarFill != null)
        {
            float expectedFillAmount = enemyStats.CurrentHealth / enemyStats.maxHealth;

            if (Mathf.Abs(healthBarFill.fillAmount - expectedFillAmount) > 0.01f)
            {
                UpdateHealthBar(enemyStats.CurrentHealth, enemyStats.maxHealth);
            }
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            float fillAmount = currentHealth / maxHealth;
            healthBarFill.fillAmount = fillAmount;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }
}
