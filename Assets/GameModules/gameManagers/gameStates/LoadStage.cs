using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadStage : AbstractState
{

    public LoadStage(IGameManInterface context) 
        : base(context)
    {
    }

    public override void StartState()
    {
        var (gamePhase, gameStage) = _gameManInter.GetGameInfo();
        SceneManager.LoadScene($"s{gameStage}Arena", LoadSceneMode.Single);

        _gameManInter.GenerateArena();
    }

    public override void UpdateState()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            _gameManInter.ChangeState(new MainMenuState(_gameManInter));
        }
    }

    public void LoadArena()
    {

    }
}