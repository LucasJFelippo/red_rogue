using UnityEngine;
using System.Collections.Generic;

public class SlashProjectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    public float speed = 10f;        // Velocidade que o slash avança
    public float lifetime = 0.3f;    // Tempo de vida curto
    public LayerMask enemyLayers;    // Layers dos inimigos

    [HideInInspector] public int damage; // Receberá o valor do PlayerAttack

    private List<GameObject> hitEnemies = new List<GameObject>();

    void Start()
    {
        // Se destrói sozinho após o tempo definido
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move para frente (baseado na rotação local)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Verifica se bateu em um inimigo (usando Bitwise operation para checar a LayerMask)
        if (((1 << other.gameObject.layer) & enemyLayers) != 0)
        {
            if (!hitEnemies.Contains(other.gameObject))
            {
                EnemyStats enemy = other.GetComponent<EnemyStats>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                hitEnemies.Add(other.gameObject);
                Debug.Log("Slash acertou: " + other.name);
            }
        }
        // Se quiser que o slash suma ao bater em paredes (opcional)
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Destroy(gameObject); 
        }
    }
}