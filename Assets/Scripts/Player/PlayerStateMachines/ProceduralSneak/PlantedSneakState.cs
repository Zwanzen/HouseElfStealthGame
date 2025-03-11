using UnityEngine;

public class PlantedSneakState : ProceduralSneakState
{
    public PlantedSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    // Private variables

    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (!Context.Player.IsGrounded)
        {
            return ProceduralSneakStateMachine.ESneakState.Stop;
        }

        if (InputManager.Instance.IsHoldingLMB)
        {
            Context.LiftedFoot = Context.LeftFoot;
            return ProceduralSneakStateMachine.ESneakState.Lifted;
        }
        
        if (InputManager.Instance.IsHoldingRMB)
        {
            Context.LiftedFoot = Context.RightFoot;
            return ProceduralSneakStateMachine.ESneakState.Lifted;
        }
        
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
        Context.MoveBody(Context.GetFeetMiddlePoint());
    }
    
}