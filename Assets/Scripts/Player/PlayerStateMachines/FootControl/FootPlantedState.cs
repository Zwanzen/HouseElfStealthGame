using UnityEngine;
using static CircleLineIntersection;

public class FootPlantedState : FootControlState
{
    public FootPlantedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if(!Context.IsFootGrounded)
            return FootControlStateMachine.EFootState.Falling;
        
        if (Context.IsFootLifting)
            return FootControlStateMachine.EFootState.Lifted;
        
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
        var dir = Vector3.zero;
        if (Context.FootGroundCast(out var hit))
        {
            var pos = hit.point + Context.FootPlaceOffset;
            dir = (pos - Context.Foot.Target.position);
        }
        Context.MoveFootToPosition(dir);
    }
}