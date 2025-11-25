using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 5f;

    [Header("Configurações de Interação")]
    public bool canBeReflected = true;
    public bool canBeDestroyed = true;

    [Header("Efeitos Visuais")]
    public GameObject explosionPrefab;

    [Header("Estado")]
    public bool isReflected = false;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = false;
            rb.linearVelocity = transform.forward * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    public void Reflect(Vector3 newDirection)
    {
        if (isReflected) return;

        if (canBeReflected)
        {
            isReflected = true;
            speed *= 1.5f;
            damage *= 2;
            CancelInvoke();
            Destroy(gameObject, 5f);

            transform.forward = newDirection;
            if (rb != null) rb.linearVelocity = transform.forward * speed;
        }
        else if (canBeDestroyed)
        {
            SpawnExplosion();
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider targetCollider)
    {
        GameObject targetObj = targetCollider.gameObject;

        if (isReflected)
        {
            if (targetObj.CompareTag("Player") || targetObj.GetComponent<SlashProjectile>()) return;

            EnemyStats enemy = targetCollider.GetComponentInParent<EnemyStats>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                SpawnExplosion();
                Destroy(gameObject);
            }
            else if (!targetObj.CompareTag("Projectile") && !targetObj.GetComponent<Projectile>())
            {
                SpawnExplosion();
                Destroy(gameObject);
            }
        }
        else
        {
            if (targetObj.CompareTag("Enemy")) return;

            if (targetObj.CompareTag("Player"))
            {
                PlayerStats playerStats = targetCollider.GetComponentInParent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damage);
                }
                SpawnExplosion();
                Destroy(gameObject);
            }
            else if (!targetObj.CompareTag("Projectile"))
            {
                SpawnExplosion();
                Destroy(gameObject);
            }
        }
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, transform.rotation);
        }
    }
}