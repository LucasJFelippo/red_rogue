using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;


public class GameManager : MonoBehaviour, IGameManInterface
{

    [Header("Game Manager")]
    private static GameManager _instance = null;

    private AbstractState _current_state;

    [Header("Player")]
    public GameObject player { get; set; }
    public GameObject playerPrefab;

    [Header("Enemies")]
    [SerializeField]
    public List<EnemyStats> spawnedEnemies = new List<EnemyStats>();

    public int gamePhase { get; set; } = 1;
    public int gameStage { get; set; } = 1;

    [Header("Modules")]
    // TODO: Not implemented yet
    IArenaGenInterface arenaObj { get; set; }
    IArenaGenInterface spawnerObj { get; set; }
    
    [Header("Getters/Setters")]
    public static IGameManInterface instance { get { return _instance; } }

    public GameObject getPlayerPrefab => playerPrefab;
    public List<EnemyStats> getSpawnedEnemies => spawnedEnemies;


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

    public Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(routine);
}