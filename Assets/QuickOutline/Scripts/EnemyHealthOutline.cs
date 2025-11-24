using UnityEngine;

[RequireComponent(typeof(Outline))]
public class EnemyHealthOutline : MonoBehaviour
{
    [Header("Dependências")]
    public EnemyStats enemyStats;

    [Header("Configurações")]
    [Range(0f, 10f)]
    public float width = 1.2f;

    private Outline outline;

    void Awake()
    {
        outline = GetComponent<Outline>();
    }

    void Start()
    {
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineWidth = width;

        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged += UpdateOutlineColor;
            // Define a cor inicial
            UpdateOutlineColor(enemyStats.CurrentHealth, enemyStats.maxHealth);
        }
        else
        {
            Debug.LogWarning("EnemyHealthOutline: EnemyStats não foi atribuído!", this);
        }
    }

    void Update()
    {
        if (outline.OutlineWidth != width)
        {
            outline.OutlineWidth = width;
        }
    }

    void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged -= UpdateOutlineColor;
        }
    }

    private void UpdateOutlineColor(float currentHealth, float maxHealth)
    {
        float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
        Color newColor;

        if (healthPercent >= 0.5f)
        {
            newColor = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
        }
        else
        {
            newColor = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
        }

        outline.OutlineColor = newColor;
    }
}