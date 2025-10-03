using UnityEngine;

using System.Collections.Generic;
using System.Net.NetworkInformation;


public class GameManager : MonoBehaviour, IGameManInterface
{

    [Header("Game Manager")]
    private static GameManager _instance = null;
    public static IGameManInterface instance { get { return _instance; } }

    private AbstractState _current_state;

    [Header("Player")]

    [Header("Enemies")]
    private List<EnemyStats> spawnedEnemies = new List<EnemyStats>();

    [Header("Map")]
    private IArenaGenInterface _arenaGen = null;

    private int gamePhase = 1;
    private int gameStage = 1;

    void Awake()
    {
        if(_instance == null){
             _instance = this;
             DontDestroyOnLoad(gameObject);
        } else {
             Destroy(this.gameObject);
             return;
        }
    }

    void Start()
    {
        ChangeState(new LoadStage(this));
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

    #region Map
    public void ChangeArenaGen(IArenaGenInterface generator)
    {
        _arenaGen = generator;
    }
    public void GenerateArena()
    {
        _arenaGen.GenerateArena();
    }
    #endregion

    public (int, int) GetGameInfo()
    {
        return (gamePhase, gameStage);
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