using UnityEngine;

public abstract class AbstractState
{

    protected IGameManInterface _gameManInter { get; }
    protected AbstractState(IGameManInterface gameManInter)
    {
        _gameManInter = gameManInter;
    }

    public abstract void StartState();
    public abstract void UpdateState();

}