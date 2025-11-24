using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyStats))]
public class EnemyDeathBlinker : MonoBehaviour
{
    [Header("Configurações de Piscada")]
    public float blinkInterval = 0.2f;
    public float startDelay = 1.5f;
    public bool accelerateBlinking = true;
    private EnemyStats enemyStats;
    private Renderer[] renderers;

    void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Start()
    {
        if (enemyStats != null)
        {
            enemyStats.OnDeath += HandleDeath;
        }
    }

    void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        float currentInterval = blinkInterval;

        while (true)
        {
            SetRenderersVisibility(false);
            yield return new WaitForSeconds(currentInterval);

            SetRenderersVisibility(true);
            yield return new WaitForSeconds(currentInterval);

            if (accelerateBlinking)
            {
                currentInterval = Mathf.Max(0.05f, currentInterval * 0.9f);
            }
        }
    }

    private void SetRenderersVisibility(bool isVisible)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = isVisible;
            }
        }
    }
}