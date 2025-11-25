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
    [SerializeField] private int minPatrolPoints = 3; // Aumentei o mínimo para garantir um circulo
    [Tooltip("The maximum number of patrol points to generate for each enemy GROUP.")]
    [SerializeField] private int maxPatrolPoints = 6;
    [Tooltip("The search radius for finding valid patrol points around a spawn tile.")]
    [SerializeField] private float patrolPointSearchRadius = 15f; // Aumente se quiser circular o mapa todo

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

        // Calculate budget logic (mantida original)
        int currentBudget = stageConfig.baseWeightBudget + (stage * 10);
        int safetyBreak = 0;

        while (currentBudget > 0 && safetyBreak < 100)
        {
            var affordableEnemies = stageConfig.enemySpawnPool
                .Where(e => e.weight <= currentBudget)
                .ToList();

            if (affordableEnemies.Count == 0) break;

            var enemyGroupToSpawn = affordableEnemies[Random.Range(0, affordableEnemies.Count)];

            // Escolhe um tile aleatório para o spawn do inimigo
            GameObject spawnTile = spawnableTiles[Random.Range(0, spawnableTiles.Count)];
            Vector3 spawnPosition = spawnTile.transform.position;

            // --- MUDANÇA: Passamos a lista de tiles para a função de patrulha ---
            Transform[] patrolPoints = GenerateLogicalPatrolPoints(spawnableTiles, spawnPosition);
            // -------------------------------------------------------------------

            GameObject spawnedGroupObject = Instantiate(enemyGroupToSpawn.enemyPrefab, spawnPosition, Quaternion.identity);
            spawnedGroupObject.transform.SetParent(enemyContainer);

            var aiComponents = spawnedGroupObject.GetComponentsInChildren<EnemyNavMeshAI>();
            foreach (var ai in aiComponents)
            {
                ai.patrolPoints = patrolPoints;
                ai.ActivateAI();
            }

            IGameManInterface gameManager = GameManager.instance;
            var enemiesStatInGroup = spawnedGroupObject.GetComponentsInChildren<EnemyStats>();
            foreach (EnemyStats enemy in enemiesStatInGroup)
            {
                enemy.gameObject.SetActive(false);
                gameManager.RegistryEnemy(enemy);
            }

            currentBudget -= enemyGroupToSpawn.weight;
            safetyBreak++;
        }
    }

    // Função totalmente reescrita para criar caminhos lógicos
    private Transform[] GenerateLogicalPatrolPoints(List<GameObject> allTiles, Vector3 centerPoint)
    {
        int pointsCount = Random.Range(minPatrolPoints, maxPatrolPoints + 1);
        List<Transform> generatedPoints = new List<Transform>();

        Transform patrolPointsParent = new GameObject($"PatrolPath_{centerPoint.GetHashCode()}").transform;
        patrolPointsParent.SetParent(patrolPointContainer);

        // 1. FILTRAGEM: Encontra tiles próximos (dentro do raio) mas ignora o tile central exato
        // Se quiser circular o MAPA TODO, aumente muito o 'patrolPointSearchRadius' no Inspector.
        var candidateTiles = allTiles
            .Where(t => Vector3.Distance(t.transform.position, centerPoint) <= patrolPointSearchRadius)
            .Where(t => Vector3.Distance(t.transform.position, centerPoint) > 2f) // Evita pontos muito colados no spawn
            .OrderBy(x => Random.value) // Embaralha para aleatoriedade na escolha
            .Take(pointsCount) // Pega a quantidade necessária
            .ToList();

        // Se não achou tiles suficientes (mapa pequeno ou raio curto), usa o que tem
        if (candidateTiles.Count == 0) candidateTiles.Add(allTiles[Random.Range(0, allTiles.Count)]);

        // 2. ORDENAÇÃO LÓGICA (Circular):
        // Ordena os pontos baseados no ângulo deles em relação ao centro de spawn.
        // Isso faz com que o caminho forme um círculo/polígono convexo ao redor do centro.
        var sortedTiles = candidateTiles.OrderBy(t =>
        {
            Vector3 dir = t.transform.position - centerPoint;
            return Mathf.Atan2(dir.z, dir.x); // Retorna o ângulo em radianos
        }).ToList();

        // 3. CRIAÇÃO DOS PONTOS
        foreach (GameObject tile in sortedTiles)
        {
            // Adiciona um pequeno offset Y para garantir que o ponto não fique enterrado no chão
            Vector3 finalPos = tile.transform.position + Vector3.up * 1.0f;

            GameObject pObj = new GameObject("PPoint");
            pObj.transform.position = finalPos;
            pObj.transform.SetParent(patrolPointsParent);
            generatedPoints.Add(pObj.transform);
        }

        return generatedPoints.ToArray();
    }
}