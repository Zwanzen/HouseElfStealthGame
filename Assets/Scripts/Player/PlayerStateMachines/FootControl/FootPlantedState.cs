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
        Context.Foot.Target.isKinematic = true;
    }

    public override void ExitState()
    {
        Context.Foot.Target.isKinematic = false;
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        //MoveToGround();
    }

    private void MoveToGround()
    {
        var dir = Vector3.zero;
        if (Context.FootGroundCast(out var hit))
        {
            var pos = hit.point + Context.FootPlaceOffset;
            dir = (pos - Context.Foot.Target.position);
        }
        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dir.magnitude;
        var breakDistance = 0.05f;
        var magLerp = mag / breakDistance;
        dir.Normalize();
        dir *= Context.SpeedCurve.Evaluate(magLerp);
        Context.MoveFootToPosition(dir);
    }
}