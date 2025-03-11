using UnityEngine;

public class LiftedSneakState : ProceduralSneakState
{
    public LiftedSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if(!InputManager.Instance.IsHoldingLMB && Context.LiftedFoot == Context.LeftFoot)
        {
            return ProceduralSneakStateMachine.ESneakState.Placing;
        }
        
        if(!InputManager.Instance.IsHoldingRMB && Context.LiftedFoot == Context.RightFoot)
        {
            return ProceduralSneakStateMachine.ESneakState.Placing;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        Context.LiftedFoot.position = Context.GroundCast(Context.LiftedFoot.position + Vector3.up * 0.5f, 2f).point + Vector3.up * 0.2f;
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