/// <summary>
/// This state is used when the foot is no longer controlled by the foot control state machine.
/// The only way to exit this state is to call TransitionToState() from the foot SM.
/// </summary>
public class FootIdleState : FootControlState
{
    public FootIdleState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key){Context = context;}
    public override FootControlStateMachine.EFootState GetNextState() {return StateKey;}
    public override void EnterState(){}
    public override void ExitState(){}
    public override void UpdateState(){}
    public override void FixedUpdateState(){}
}