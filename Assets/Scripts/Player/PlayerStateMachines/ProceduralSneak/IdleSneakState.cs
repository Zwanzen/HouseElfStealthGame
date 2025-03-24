using UnityEngine;

// DEPENDING ON WHAT CONTROL SYSTEMS ARE USED AND IN WHAT ORDER THEY ARE USED, THIS STATE MAY BE UNNECESSARY
public class IdleSneakState : ProceduralSneakState
{
    public IdleSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (Context.Player.IsGrounded)
        {
            return ProceduralSneakStateMachine.ESneakState.Start;
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
        // Set the feet target positions to the feet positions
        Context.LeftFoot.position = Context.BodyIK.references.leftFoot.position;
        Context.RightFoot.position = Context.BodyIK.references.rightFoot.position;
    }
}