using UnityEngine;
using static RigidbodyMovement;

public class LiftedSneakState : ProceduralSneakState
{
    public LiftedSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if(!InputManager.Instance.IsHoldingLMB && Context.LiftedFoot == Context.LeftFoot)
        {
            return ProceduralSneakStateMachine.ESneakState.Placing;
        }
        
        if(!InputManager.Instance.IsHoldingRMB && Context.LiftedFoot == Context.RightFoot)
        {
            return ProceduralSneakStateMachine.ESneakState.Placing;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        Context.LiftedFoot.position = Context.GroundCast(Context.LiftedFoot.position + Vector3.up * 0.5f, 2f).point + Vector3.up * 0.2f;
        
        // Temp
        Context.ResetBodyGoalVel();
        ResetFootsVelocities();
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        MoveFeet();
        
        // Find the point between the lifted and planted foot
        Vector3 lerpPosition = Vector3.Lerp(Context.PlantedFoot.position, Context.LiftedFoot.position, 0.2f);
        Context.MoveBody(lerpPosition);
    }
    
    private Vector3 _sPlantedFootGoalVel;
    private Vector3 _sLiftedFootGoalVel;
    private void ResetFootsVelocities()
    {
        _sPlantedFootGoalVel = Context.PlantedFoot.linearVelocity;
        _sLiftedFootGoalVel = Context.LiftedFoot.linearVelocity;
    }

    private void MoveFeet()
    {
        // Get the planted foot ground position
        var plantedFootGroundPos = Context.GetFootGroundPosition(Context.PlantedFoot);
        
        // Get the desired direction to move both feet
        var plantedDirection = plantedFootGroundPos - Context.PlantedFoot.position;
        var liftedDirection = Context.Player.RelativeMoveInput;
        
        Debug.DrawLine(Context.PlantedFoot.position, Context.PlantedFoot.position + (liftedDirection.normalized * Context.SneakStepLength), Color.white);
        var footDir = Context.LiftedFoot.position - Context.PlantedFoot.position;
        footDir.y = 0;
        var pos = Context.LiftedFoot.position + liftedDirection;
        pos.y = 0;
        var dist = Vector3.Distance(Context.PlantedFoot.position, pos);
        // Clamp the lifted foot direction to not pass the step length
        if (dist > Context.SneakStepLength)
        {
            pos = Vector3.ClampMagnitude(footDir + liftedDirection, Context.SneakStepLength);
            pos += Context.PlantedFoot.position;
            liftedDirection = pos - Context.LiftedFoot.position;
        }
        
        
        // Move the feet to their grounded positions
        _sPlantedFootGoalVel = MoveRigidbody(Context.PlantedFoot, plantedDirection, _sPlantedFootGoalVel, Context.SneakMovementSettings);
        _sLiftedFootGoalVel = MoveRigidbody(Context.LiftedFoot, liftedDirection, _sLiftedFootGoalVel, Context.SneakMovementSettings);
    }
}