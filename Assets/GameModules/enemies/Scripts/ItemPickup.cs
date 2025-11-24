using UnityEngine;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType { Health, Speed, Damage }

    [Header("Configurações do Item")]
    public ItemType type;
    public float value = 20f;
    public float duration = 0f;

    [Header("Configurações de Despawn")]
    public float lifeTime = 15f;
    public float blinkDuration = 5f;
    public float blinkSpeed = 10f;

    [Header("Visual")]
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1f;

    private Vector3 startPos;
    private Renderer[] renderers;

    void Start()
    {
        startPos = transform.position;
        renderers = GetComponentsInChildren<Renderer>();
        StartCoroutine(LifeCycleRoutine());
    }

    void Update()
    {
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject);
            Destroy(gameObject);
        }
    }

    private IEnumerator LifeCycleRoutine()
    {
        yield return new WaitForSeconds(lifeTime - blinkDuration);
        float timer = 0f;
        while (timer < blinkDuration)
        {
            timer += Time.deltaTime;
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            bool isVisible = blink > 0.5f;

            SetRenderersVisible(isVisible);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
    }

    private void ApplyEffect(GameObject player)
    {
        switch (type)
        {
            case ItemType.Health:
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null) stats.Heal(value);
                break;

            case ItemType.Speed:
                var movement = player.GetComponent<PlayerMovement>();
                if (movement != null) movement.ApplySpeedBoost(value, duration);
                break;

            case ItemType.Damage:
                var attack = player.GetComponent<PlayerAttack>();
                if (attack != null) attack.ApplyDamageBoost(value, duration);
                break;
        }

        var buffManager = player.GetComponent<PlayerBuffManager>();
        if (buffManager != null)
        {
            buffManager.AddBuff(type, duration);
        }
    }
}