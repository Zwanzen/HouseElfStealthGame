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
        
        /*
    // Get the direction to the lifted foot from the planted foot
    var footDir = Context.LiftedFoot.position - Context.PlantedFoot.position;
    // Get the dot product of the lifted direction and foot direction
    var dot = Vector3.Dot(liftedDirection, footDir);
    // Now we can get the relative direction distance to be used to clamp the lifted foot
    var relativeDistance = footDir.magnitude * dot;


    // Clamp the lifted foot direction to not pass the step length
    if (relativeDistance > Context.SneakStepLength)
    {
        liftedDirection = liftedDirection.normalized * (Context.SneakStepLength - relativeDistance);
    }
    */
        
        // Get the potential step
        var step = liftedDirection + Context.LiftedFoot.position;
        // Get the distance from the planted foot to the potential step
        var stepDistance = Vector3.Distance(Context.PlantedFoot.position, step);
        // Get the distance between the planted foot and the lifted foot
        var footDistance = Vector3.Distance(Context.PlantedFoot.position, Context.LiftedFoot.position);
        var footDir = Context.LiftedFoot.position - Context.PlantedFoot.position;
        var dot = Vector3.Dot(liftedDirection, footDir);
        var relativeDistance = footDir.magnitude * dot;
        
        Debug.DrawLine(Context.LiftedFoot.position, Context.LiftedFoot.position + liftedDirection, Color.red);
        // Clamp the lifted foot direction to not pass the step length
        if (relativeDistance > Context.SneakStepLength * 0.95f)
        {
            liftedDirection = liftedDirection.normalized * (Context.SneakStepLength - relativeDistance);
        }
        Debug.DrawLine(Context.LiftedFoot.position, Context.LiftedFoot.position + liftedDirection, Color.green);

        
        
        
        // Move the feet to their grounded positions
        _sPlantedFootGoalVel = MoveRigidbody(Context.PlantedFoot, plantedDirection, _sPlantedFootGoalVel, Context.SneakMovementSettings);
        _sLiftedFootGoalVel = MoveRigidbody(Context.LiftedFoot, liftedDirection, _sLiftedFootGoalVel, Context.SneakMovementSettings);
    }
}