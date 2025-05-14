using UnityEngine;

public class FootFallingState : FootControlState
{
    public FootFallingState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }
    
    public override FootControlStateMachine.EFootState GetNextState()
    {
        if(Context.IsFootGrounded)
            return FootControlStateMachine.EFootState.Planted;

        return StateKey;
    }
    
    public override void EnterState()
    {
        Context.Foot.Target.useGravity = true;
    }

    public override void ExitState()
    {
        Context.Foot.Target.useGravity = false;
        Context.PlayFootSound();
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
    } 

}