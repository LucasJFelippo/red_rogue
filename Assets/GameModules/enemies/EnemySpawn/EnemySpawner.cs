using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The configuration asset that defines the spawning rules for this stage.")]
    public StageSpawnConfig stageConfig;

    [Header("Spawning Parameters")]
    [Tooltip("The minimum number of patrol points to generate for each enemy GROUP.")]
    [SerializeField] private int minPatrolPoints = 2;
    [Tooltip("The maximum number of patrol points to generate for each enemy GROUP.")]
    [SerializeField] private int maxPatrolPoints = 5;
    [Tooltip("The search radius for finding valid patrol points around a spawn tile.")]
    [SerializeField] private float patrolPointSearchRadius = 10f;

    private Transform enemyContainer;
    private Transform patrolPointContainer;

    public void SpawnEnemies(List<GameObject> spawnableTiles, int stage)
    {
        if (stageConfig == null)
        {
            Debug.LogError("StageConfig is not assigned in the EnemySpawner!", this);
            return;
        }

        if (enemyContainer == null)
        {
            enemyContainer = new GameObject("SpawnedEnemies").transform;
            enemyContainer.SetParent(this.transform);
        }

        if (patrolPointContainer == null)
        {
            patrolPointContainer = new GameObject("GeneratedPatrolPoints").transform;
            patrolPointContainer.SetParent(this.transform);
        }

        if (spawnableTiles.Count == 0)
        {
            Debug.LogWarning("No spawnable tiles found. Cannot spawn enemies.", this);
            return;
        }

        // TODO: MAGIC STUFF, CHANGE LATER
        int currentBudget = (int)(stageConfig.baseWeightBudget * Mathf.Pow(2f, (stage - 1) / 4f));
        int safetyBreak = 0;

        while (currentBudget > 0 && spawnableTiles.Count > 0 && safetyBreak < 100)
        {
            List<EnemySpawnData> affordableEnemies = stageConfig.enemySpawnPool
                .Where(e => e.weight <= currentBudget)
                .ToList();

            if (affordableEnemies.Count == 0) break;

            EnemySpawnData enemyGroupToSpawn = affordableEnemies[Random.Range(0, affordableEnemies.Count)];

            int tileIndex = Random.Range(0, spawnableTiles.Count);
            GameObject randomTile = spawnableTiles[tileIndex];
            spawnableTiles.RemoveAt(tileIndex);

            Vector3 spawnPosition = randomTile.transform.position + new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));

            // Spawn the group prefab
            GameObject spawnedGroupObject = Instantiate(enemyGroupToSpawn.enemyPrefab, spawnPosition, Quaternion.identity, enemyContainer);

            // Generate ONE set of patrol points for the entire group
            Transform[] patrolPoints = GeneratePatrolPointsForGroup(spawnedGroupObject, spawnPosition);

            // Find all individual enemies within the group and assign them the SAME patrol points
            EnemyNavMeshAI[] enemiesInGroup = spawnedGroupObject.GetComponentsInChildren<EnemyNavMeshAI>();
            foreach (EnemyNavMeshAI enemy in enemiesInGroup)
            {
                enemy.patrolPoints = patrolPoints;
                enemy.ActivateAI();
            }

            currentBudget -= enemyGroupToSpawn.weight;
            safetyBreak++;
        }
    }

    private Transform[] GeneratePatrolPointsForGroup(GameObject groupObject, Vector3 spawnOrigin)
    {
        int patrolPointCount = Random.Range(minPatrolPoints, maxPatrolPoints + 1);
        List<Transform> generatedPoints = new List<Transform>();

        Transform patrolPointsParent = new GameObject($"{groupObject.name}_PatrolPoints").transform;
        patrolPointsParent.SetParent(patrolPointContainer);

        for (int i = 0; i < patrolPointCount; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolPointSearchRadius;
            randomDirection += spawnOrigin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolPointSearchRadius, NavMesh.AllAreas))
            {
                GameObject patrolPointObj = new GameObject($"PatrolPoint_{i}");
                patrolPointObj.transform.position = hit.position;
                patrolPointObj.transform.SetParent(patrolPointsParent);
                generatedPoints.Add(patrolPointObj.transform);
            }
        }

        return generatedPoints.ToArray();
    }
}