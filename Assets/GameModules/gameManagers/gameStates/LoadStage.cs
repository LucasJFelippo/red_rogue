using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;

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

        List<GameObject> floorTiles = arenaObj.GetFloorTiles();

        EnemySpawner enemySpw = GameObject.FindWithTag("EnemyController").GetComponent<EnemySpawner>();
        enemySpw.SpawnEnemies(floorTiles, gameStage);

        float spawnX = UnityEngine.Random.Range(0, arenaObj.ArenaWidth);
        float spawnY = UnityEngine.Random.Range(0, arenaObj.ArenaDepth);

        GameObject randomTile = floorTiles[Random.Range(0, floorTiles.Count)];
        GameObject player = Object.Instantiate(_gameManInter.getPlayerPrefab, randomTile.transform.position, randomTile.transform.rotation);

        CameraController cameraControl = GameObject.FindWithTag("MainCamera").GetComponent<CameraController>();
        cameraControl.target = player.transform;

    }
}