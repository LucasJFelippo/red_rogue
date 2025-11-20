using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;

public class CompletedStage : AbstractState
{
    public CompletedStage(IGameManInterface context) 
        : base(context)
    {
        bool _stageCompleted = false;
    }

    public override void StartState()
    {
        Debug.Log("Entering Completed Stage state");
        _gameManInter.StartCoroutine(CompleteStageRoutine());
    }

    public override void UpdateState()
    {
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

    private IEnumerator CompleteStageRoutine() {
        IArenaGenInterface arenaObj = GameObject.FindWithTag("Arena").GetComponent<IArenaGenInterface>();
        yield return arenaObj.StageCompletedAnimation();
    }
}