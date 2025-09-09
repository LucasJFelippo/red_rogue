using UnityEngine;

public interface IGameManInterface
{

    // Game Manager
    void ChangeState(AbstractState newState);

    // Enemy
    void RegistryEnemy(EnemyStats enemy);
    void UnregistryEnemy(EnemyStats enemy);

}
