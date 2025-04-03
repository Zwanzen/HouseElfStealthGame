public class FootFallingState : FootControlState
{
    public FootFallingState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }
    
    public override FootControlStateMachine.EFootState GetNextState()
    {
        return Context.IsFootGrounded ? FootControlStateMachine.EFootState.Planted : StateKey;
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
        Context.MoveFootToPosition(Context.Foot.RestTarget.position);
    } 
}