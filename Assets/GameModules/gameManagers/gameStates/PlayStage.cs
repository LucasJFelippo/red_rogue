using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;

public class PlayStage : AbstractState
{
    public PlayStage(IGameManInterface context) 
        : base(context)
    {
        bool _stageCompleted = false;
    }

    public override void StartState()
    {
        Debug.Log("Entering Play Stage state");
        GameObject player = _gameManInter.player;

        PlayerMovement playerMove = player.GetComponent<PlayerMovement>();
        playerMove.enabled = true;
    }

    public override void UpdateState()
    {
        if (_gameManInter.getSpawnedEnemies.Count == 0) {
            // _gameManInter.StartCoroutine(CompleteStageRoutine())
            if (Input.GetKeyDown(KeyCode.E)) {
                _gameManInter.gamePhase += 1;
                if (_gameManInter.gamePhase > 5) {
                    _gameManInter.gamePhase = 1;
                    _gameManInter.gameStage += 1;
                }
                Debug.Log($"Player going to phase {_gameManInter.gamePhase} stage {_gameManInter.gameStage}");
                _gameManInter.ChangeState(new LoadStage(_gameManInter));
            }
        }
    }

    // private IEnumerator CompleteStageRoutine() {
    //     _gameManInter.gamePhase += 1;
    //     if (_gameManInter.gamePhase > 5) {
    //         _gameManInter.gamePhase = 1;
    //         _gameManInter.gameStage += 1;
    //     }
    // }
}