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
        Context.ResetLiftedFootGoalVel();
        Context.ResetBodyGoalVel();
    }

    public override void ExitState()
    {
        _placed = false;
    }

    public override void UpdateState()
    {
        if(Context.FeetIsGrounded())
        {
            _placed = true;
        }
    }

    public override void FixedUpdateState()
    {
        // Ground Pos
        var groundPos = Context.GetFootGroundPosition(Context.LiftedFoot);
        var direction = groundPos - Context.LiftedFoot.position;
        Context.MoveLiftedFoot(direction);
        Context.MoveBody(Context.GetFeetMiddlePoint());
    }
}