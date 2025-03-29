public abstract class FootControlState : BaseState<FootControlStateMachine.EFootState>
{
    protected FootControlContext Context;

    public FootControlState(FootControlContext context, FootControlStateMachine.EFootState key) : base(key)
    {
        Context = context;
    }
}