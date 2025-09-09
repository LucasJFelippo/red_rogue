using UnityEngine;

using System.Collections.Generic;


public class GameManager : MonoBehaviour, IGameManInterface
{

    [Header("Game Manager")]
    private AbstractState _current_state;

    public static GameManager instance = null;

    [Header("Player")]

    [Header("Enemies")]
    private List<EnemyStats> spawnedEnemies = new List<EnemyStats>();


    void Awake()
    {
        if(instance == null){
             instance = this;
             DontDestroyOnLoad(gameObject);
        } else {
             Destroy(this.gameObject);
             return;
        }
    }

    void Start()
    {
        ChangeState(new MainMenuState(this));
    }

    void Update()
    {
        _current_state.UpdateState();
    }

    public void ChangeState(AbstractState newState)
    {
        _current_state = newState;
        _current_state.StartState();
    }

    public void RegistryEnemy(EnemyStats enemy)
    {
        spawnedEnemies.Add(enemy);
    }

    public void UnregistryEnemy(EnemyStats enemy)
    {
        bool removed = spawnedEnemies.Remove(enemy);
    }
}