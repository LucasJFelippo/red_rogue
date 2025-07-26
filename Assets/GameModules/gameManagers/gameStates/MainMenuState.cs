using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuState : AbstractState
{

    public MainMenuState(IGameManInterface context) 
        : base(context)
    {
    }

    public override void StartState()
    {
        Debug.Log("Loading Main Menu");
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    public override void UpdateState()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SceneManager.LoadScene("testScene", LoadSceneMode.Single);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("Pressed D");
        }
    }
}
