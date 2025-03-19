using UnityEngine;

public class PlacingSneakState : ProceduralSneakState
{
    public PlacingSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }

    private bool _placed;
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (_placed)
        {
            return ProceduralSneakStateMachine.ESneakState.Planted;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        _startPos = Context.LiftedFoot.position;
        Context.ResetLiftedFootGoalVel();
        Context.ResetBodyGoalVel();
    }

    public override void ExitState()
    {
        Context.PlaySound(Context.LiftedFoot,Context.GetGroundTypeFromFoot(Context.LiftedFoot));
        _placed = false;
    }

    public override void UpdateState()
    {
        if(Context.FeetIsGrounded())
        {
            _placed = true;
        }
    }

    private Vector3 _startPos;
    
    public override void FixedUpdateState()
    {
        // Ground Pos
        var groundPos = Context.GetFootGroundPosition(Context.LiftedFoot);
        var direction = groundPos - Context.LiftedFoot.position;
        
        var maxDist = Vector3.Distance(groundPos, _startPos);
        var distToPos = Vector3.Distance(Context.LiftedFoot.position, groundPos);
        var lerp = distToPos / maxDist;

        var maxDirection = direction.normalized * Context.SneakStepLength;
        var lerpedDirection = Vector3.Lerp(direction, maxDirection, Context.SpeedCurve.Evaluate(lerp));
        
        Context.MoveLiftedFoot(lerpedDirection);
        Context.MoveBody(Context.GetFeetMiddlePoint());
    }
}