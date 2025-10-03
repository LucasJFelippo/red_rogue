using UnityEngine;

public interface IGameManInterface
{

    // Game Manager
    void ChangeState(AbstractState newState);
    (int, int) GetGameInfo();

    // Map
    void ChangeArenaGen(IArenaGenInterface generator);
    void GenerateArena();

    // Enemy
    void RegistryEnemy(EnemyStats enemy);
    void UnregistryEnemy(EnemyStats enemy);

}
