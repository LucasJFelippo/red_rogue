using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStageConfig", menuName = "Roguelike/Stage Spawn Config", order = 1)]
public class StageSpawnConfig : ScriptableObject
{
    [Tooltip("The total 'cost' of all enemies that can be spawned in this stage.")]
    public int baseWeightBudget = 100;

    [Tooltip("The list of all possible enemies that can appear in this stage and their associated 'cost'.")]
    public List<EnemySpawnData> enemySpawnPool;
}

[System.Serializable]
public class EnemySpawnData
{
    [Tooltip("The enemy prefab to be spawned.")]
    public GameObject enemyPrefab;

    [Tooltip("The 'cost' of spawning this enemy. Higher weight means tougher or rarer enemies.")]
    [Min(1)]
    public int weight = 10;
}