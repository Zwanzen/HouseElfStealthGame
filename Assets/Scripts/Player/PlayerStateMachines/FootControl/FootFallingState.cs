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
        var xzFootPos = Context.Foot.Target.position;
        xzFootPos.y = 0f;
        var xzRestPos = Context.Foot.RestTarget.position;
        xzRestPos.y = 0f;
        var dirToRest = xzRestPos - xzFootPos;
        var dir = Vector3.down;
        // Check if the foot is stuck on a ledge
        if (Context.CheckStuckOnLedge(out var stuckHit) && !Context.IsFootGrounded)
        {
            // If the foot is stuck on a ledge, move it away
            var other = stuckHit.collider.ClosestPoint(Context.Foot.Target.position);
            dir = (Context.Foot.Target.position + Vector3.down - other).normalized;
            Context.MoveFootToPosition(dir);
            return;
        }
        var dirMag = dirToRest.magnitude;
        dir = Vector3.Lerp(dir, dirToRest + Vector3.down, dirMag);

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