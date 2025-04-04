using UnityEngine;

public class FootLiftedState : FootControlState
{
    public FootLiftedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if (!Context.IsFootLifting)
            return FootControlStateMachine.EFootState.Falling;
        
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
        var pos = Context.Foot.Target.position + Vector3.up * 0.15f + Context.Player.RelativeMoveInput.normalized;
        var dir = (pos - Context.Foot.Target.position);
        Context.MoveFootToPosition(dir);
    }
}