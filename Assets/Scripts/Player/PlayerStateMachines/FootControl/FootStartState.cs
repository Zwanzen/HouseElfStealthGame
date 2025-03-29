public class FootStartState : FootControlState
{
    public FootStartState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
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