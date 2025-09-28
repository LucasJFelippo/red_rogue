using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The configuration asset that defines the spawning rules for this stage.")]
    public StageSpawnConfig stageConfig;
    [Tooltip("A reference to the floor generator to get the list of valid floor tiles.")]
    public floorGen floorGenerator;

    [Header("Spawning Parameters")]
    [Tooltip("The minimum number of patrol points to generate for each enemy.")]
    [SerializeField] private int minPatrolPoints = 2;
    [Tooltip("The maximum number of patrol points to generate for each enemy.")]
    [SerializeField] private int maxPatrolPoints = 5;
    [Tooltip("The search radius for finding valid patrol points around a spawn tile.")]
    [SerializeField] private float patrolPointSearchRadius = 10f;

    public void SpawnEnemies()
    {
        if (stageConfig == null || floorGenerator == null)
        {
            Debug.LogError("StageConfig or FloorGenerator is not assigned in the EnemySpawner!", this);
            return;
        }

        List<GameObject> spawnableTiles = floorGenerator.GetFloorTiles();
        if (spawnableTiles.Count == 0)
        {
            Debug.LogWarning("No spawnable tiles found. Cannot spawn enemies.", this);
            return;
        }

        int currentBudget = stageConfig.totalWeightBudget;
        int safetyBreak = 0;

        while (currentBudget > 0 && safetyBreak < 100)
        {
            List<EnemySpawnData> affordableEnemies = new List<EnemySpawnData>();
            foreach (var enemyData in stageConfig.enemySpawnPool)
            {
                if (enemyData.weight <= currentBudget)
                {
                    affordableEnemies.Add(enemyData);
                }
            }

            if (affordableEnemies.Count == 0) break;

            EnemySpawnData enemyToSpawn = affordableEnemies[Random.Range(0, affordableEnemies.Count)];

            if (enemyToSpawn == null) break;

            GameObject randomTile = spawnableTiles[Random.Range(0, spawnableTiles.Count)];
            Vector3 spawnPosition = randomTile.transform.position + new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));

            GameObject spawnedEnemyObj = Instantiate(enemyToSpawn.enemyPrefab, spawnPosition, Quaternion.identity);

            GeneratePatrolPoints(spawnedEnemyObj, randomTile.transform.position);

            currentBudget -= enemyToSpawn.weight;
            safetyBreak++;
        }
    }

    private void GeneratePatrolPoints(GameObject enemyObject, Vector3 origin)
    {
        EnemyNavMeshAI enemyAI = enemyObject.GetComponent<EnemyNavMeshAI>();
        if (enemyAI == null) return;

        int patrolPointCount = Random.Range(minPatrolPoints, maxPatrolPoints + 1);
        List<Transform> generatedPoints = new List<Transform>();

        for (int i = 0; i < patrolPointCount; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolPointSearchRadius;
            randomDirection += origin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolPointSearchRadius, 1))
            {
                GameObject patrolPointObj = new GameObject($"PatrolPoint_{enemyObject.name}_{i}");
                patrolPointObj.transform.position = hit.position;
                patrolPointObj.transform.SetParent(enemyObject.transform);
                generatedPoints.Add(patrolPointObj.transform);
            }
        }

        enemyAI.patrolPoints = generatedPoints.ToArray();
    }
}

