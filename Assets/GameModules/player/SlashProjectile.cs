using UnityEngine;
using System.Collections.Generic;

public class SlashProjectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    public float speed = 10f;
    public float lifetime = 0.3f;
    public LayerMask enemyLayers;

    [HideInInspector] public int damage;

    private List<GameObject> hitEnemies = new List<GameObject>();

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Verifica se a layer do objeto tocado está na máscara 'enemyLayers'
        if (((1 << other.gameObject.layer) & enemyLayers) != 0)
        {
            // Evita acertar o mesmo inimigo 2x no mesmo ataque
            // Usamos a raiz (root) do objeto para garantir unicidade
            GameObject rootObject = other.transform.root.gameObject;

            if (!hitEnemies.Contains(rootObject))
            {
                // --- CORREÇÃO PRINCIPAL AQUI ---
                // Mudamos de GetComponent para GetComponentInParent
                // Isso sobe a hierarquia até achar o script, não importa onde bateu
                EnemyStats enemy = other.GetComponentInParent<EnemyStats>();

                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    hitEnemies.Add(rootObject); // Registra que este inimigo já tomou dano
                    Debug.Log("Slash acertou: " + enemy.name);
                }
            }
        }
    }
}