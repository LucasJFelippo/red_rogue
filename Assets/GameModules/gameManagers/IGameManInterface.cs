using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IGameManInterface
{

    // Player
    GameObject getPlayerPrefab { get; }
    GameObject player { get; set; }

    // Game Manager
    void ChangeState(AbstractState newState);
    (int, int) GetGameInfo();

    int gamePhase { get; set; }
    int gameStage { get; set; }

    // Enemy
    List<EnemyStats> getSpawnedEnemies { get; }
    void RegistryEnemy(EnemyStats enemy);
    void UnregistryEnemy(EnemyStats enemy);

    // Handlers
    Coroutine StartCoroutine(IEnumerator routine);
}