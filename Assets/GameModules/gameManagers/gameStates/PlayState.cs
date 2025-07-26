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
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    public override void UpdateState()
    {

    }
}
