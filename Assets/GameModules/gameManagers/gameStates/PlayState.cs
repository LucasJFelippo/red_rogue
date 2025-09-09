using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayState : AbstractState
{

    public PlayState(IGameManInterface context) 
        : base(context)
    {
    }

    public override void StartState()
    {
        SceneManager.LoadScene("testScene", LoadSceneMode.Single);
    }

    public override void UpdateState()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            _gameManInter.ChangeState(new MainMenuState(_gameManInter));
        }
    }
}