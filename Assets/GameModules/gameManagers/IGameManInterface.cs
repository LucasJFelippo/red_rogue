using UnityEngine;

public interface IGameManInterface
{

    // Game Manager
    void ChangeState(AbstractState newState);
    (int, int) GetGameInfo();

    // Enemy
    void RegistryEnemy(EnemyStats enemy);
    void UnregistryEnemy(EnemyStats enemy);

}
