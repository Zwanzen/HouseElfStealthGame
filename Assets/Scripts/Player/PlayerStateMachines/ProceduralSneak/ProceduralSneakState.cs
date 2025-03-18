using UnityEngine;

public abstract class ProceduralSneakState : BaseState<ProceduralSneakStateMachine.ESneakState>
{
        
    protected ProceduralSneakContext Context;

    public ProceduralSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(key)
    {
        Context = context;
    }
    
}