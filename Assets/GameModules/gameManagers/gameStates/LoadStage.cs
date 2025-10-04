using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections;

public class LoadStage : AbstractState
{

    public LoadStage(IGameManInterface context) 
        : base(context)
    {
    }

    public override void StartState()
    {
        _gameManInter.StartCoroutine(LoadStageRoutine());
    }

    public override void UpdateState()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            _gameManInter.ChangeState(new MainMenuState(_gameManInter));
        }
    }

    private IEnumerator LoadStageRoutine()
    {
        var (gamePhase, gameStage) = _gameManInter.GetGameInfo();
        var sceneLoad = SceneManager.LoadSceneAsync($"s{gameStage}Arena", LoadSceneMode.Single);

        yield return sceneLoad;

        IArenaGenInterface arenaObj = GameObject.FindWithTag("Arena").GetComponent<IArenaGenInterface>();
        arenaObj.GenerateArena();
        arenaObj.GenerateNavMesh();

        EnemySpawner enemySpw = GameObject.FindWithTag("EnemyController").GetComponent<EnemySpawner>();
        enemySpw.SpawnEnemies(arenaObj.GetFloorTiles(), gameStage);

    }
}