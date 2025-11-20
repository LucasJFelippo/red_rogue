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
        Debug.Log("Entering Load Stage state");
        _gameManInter.StartCoroutine(LoadStageRoutine());
    }

    public override void UpdateState()
    {

    }

    private IEnumerator LoadStageRoutine()
    {
        _gameManInter.GetChild(0).gameObject.SetActive(true);

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
        _gameManInter.player = player;

        CameraController cameraControl = GameObject.FindWithTag("MainCamera").GetComponent<CameraController>();
        cameraControl.target = player.transform;

        player.SetActive(false);

        _gameManInter.GetChild(0).gameObject.SetActive(false);

        // Animation Phase

        yield return arenaObj.SpawnAnimation();

        yield return new WaitForSeconds(2);

        PlayerMovement playerMove = player.GetComponent<PlayerMovement>();
        playerMove.enabled = false;
        player.SetActive(true);

        foreach (EnemyStats enemy in _gameManInter.getSpawnedEnemies) {
            enemy.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.2f);
        }

        _gameManInter.ChangeState(new PlayStage(_gameManInter));

    }
}