using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IGameManInterface
{

    GameObject getPlayerPrefab { get; }

    // Game Manager
    void ChangeState(AbstractState newState);
    (int, int) GetGameInfo();

    // Enemy
    List<EnemyStats> getSpawnedEnemies { get; }
    void RegistryEnemy(EnemyStats enemy);
    void UnregistryEnemy(EnemyStats enemy);

    // Handlers
    Coroutine StartCoroutine(IEnumerator routine);
}