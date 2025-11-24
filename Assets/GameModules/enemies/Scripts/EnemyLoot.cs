using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyStats))]
public class EnemyLootDropper : MonoBehaviour
{
    [System.Serializable]
    public class LootItem
    {
        public string name;
        public GameObject itemPrefab;
        [Tooltip("Chance relativa deste item cair.")]
        [Range(1, 100)] public int weight = 10;
    }

    [Header("Configurações de Drop")]
    [Range(0, 100)]
    public float globalDropChance = 30f;

    [Tooltip("Lista de itens possíveis e seus pesos.")]
    public List<LootItem> lootTable;

    private EnemyStats enemyStats;
    private bool hasDied = false;
    private bool isQuitting = false;

    void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
    }

    void Start()
    {
        if (enemyStats != null)
        {
            enemyStats.OnDeath += MarkAsDead;
        }
    }
    void OnApplicationQuit()
    {
        isQuitting = true;
    }
    void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnDeath -= MarkAsDead;
        }
        if (hasDied && !isQuitting)
        {
            TryDropLoot();
        }
    }

    private void MarkAsDead()
    {
        hasDied = true;
    }

    private void TryDropLoot()
    {
        float roll = Random.Range(0f, 100f);
        if (roll > globalDropChance) return;

        GameObject itemToSpawn = GetRandomItemFromTable();

        if (itemToSpawn != null)
        {
            Instantiate(itemToSpawn, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }

    private GameObject GetRandomItemFromTable()
    {
        if (lootTable.Count == 0) return null;

        int totalWeight = 0;
        foreach (var item in lootTable)
        {
            totalWeight += item.weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeightSum = 0;

        foreach (var item in lootTable)
        {
            currentWeightSum += item.weight;
            if (randomValue < currentWeightSum)
            {
                return item.itemPrefab;
            }
        }

        return lootTable[0].itemPrefab;
    }
}