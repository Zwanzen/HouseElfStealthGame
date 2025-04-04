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
        Context.ResetGoalVelocity();
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        var footPos = Context.Foot.Target.position;
        footPos.y = Context.OtherFoot.Target.position.y + 0.15f;
        var pos = footPos + Context.Player.RelativeMoveInput.normalized;
        var dir = (pos - Context.Foot.Target.position);
        Context.MoveFootToPosition(dir);
    }
}