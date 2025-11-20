using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;

public interface IStageFinisher
{
    void FinishStage();
}

public class PlayStage : AbstractState, IStageFinisher
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
    }

    public void FinishStage()
    {
        _gameManInter.ChangeState(new CompletedStage(_gameManInter));
    }
}