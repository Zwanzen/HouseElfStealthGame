using UnityEngine;

public class FootPlacingState : FootControlState
{
    public FootPlacingState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
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
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        MoveToGround();
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
        var breakDistance = 0.1f;
        var magLerp = mag / breakDistance;
        dir.Normalize();
        dir *= Context.SpeedCurve.Evaluate(magLerp);
        
        Context.MoveFootToPosition(dir);
    }
}