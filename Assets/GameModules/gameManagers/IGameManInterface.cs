using UnityEngine;
using System.Collections;

public interface IGameManInterface
{

    GameObject getPlayerPrefab { get; }

    // Game Manager
    void ChangeState(AbstractState newState);
    (int, int) GetGameInfo();

    // Enemy
    void RegistryEnemy(EnemyStats enemy);
    void UnregistryEnemy(EnemyStats enemy);

    // Handlers
    Coroutine StartCoroutine(IEnumerator routine);
}
