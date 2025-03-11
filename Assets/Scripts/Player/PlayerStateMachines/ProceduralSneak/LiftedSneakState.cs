using UnityEngine;

public class LiftedSneakState : ProceduralSneakState
{
    public LiftedSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        return StateKey;
    }

    public override void EnterState()
    {
        
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        
    }
}