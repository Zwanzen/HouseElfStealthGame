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
    
    private Vector3 _bodyDirection;

    private void MoveFeet()
    {
        // Get the planted foot ground position
        var plantedFootGroundPos = Context.GetFootGroundPosition(Context.PlantedFoot);
        
        // Get the desired direction to move both feet
        var plantedDirection = plantedFootGroundPos - Context.PlantedFoot.position;
        var liftedDirection = Context.Player.RelativeMoveInput;
        
        // Set lifted direction magnitude to step length
        liftedDirection = liftedDirection.normalized * Context.SneakStepLength;
        
        // Update body and foot rot if input is not zero
        if (liftedDirection.magnitude > 0.01f)
        {
            Context.UpdateBodyRotation(Context.Player.Camera.GetCameraYawTransform().forward);
            Context.UpdateFootRotation(Context.LiftedFoot);
        }
        
        // Get both foot positions
        var liftedFootPos = Context.LiftedFoot.position;
        liftedFootPos.y = 0;
        var plantedFootPos = Context.PlantedFoot.position;
        plantedFootPos.y = 0;
        
        var footDir = liftedFootPos - plantedFootPos;
        var pos = liftedFootPos + liftedDirection;
        
        // Add y value
        var liftHeight = Vector3.up * 0.2f;
        if (InputManager.Instance.IsLifting)
        {
            liftHeight += Vector3.up * 0.15f;
        }
        var yPos = Context.GroundCast(Context.LiftedFoot.position + Vector3.up * 0.5f, 2f).point + liftHeight;
        yPos.x = 0;
        yPos.z = 0;
        // Add the y value to the walk pos
        pos += yPos;
        var dirToPos = pos - Context.LiftedFoot.position;
        liftedDirection = dirToPos;
        
        
        // Get the distance to the lifted foot direction from the planted foot
        var dist = Vector3.Distance(plantedFootPos, pos);

        if(dist > Context.SneakStepLength)
        {
            // Clamp the lifted foot direction to not pass the step length
            pos = Vector3.ClampMagnitude(footDir + liftedDirection, Context.SneakStepLength);
            pos += plantedFootPos;
            dirToPos = pos - liftedFootPos;
            if(dirToPos.magnitude > Context.SneakStepLength)
            {
                dirToPos = dirToPos.normalized * Context.SneakStepLength;
            }
        
            var maxDist = Context.SneakStepLength;
            var distToPos = Vector3.Distance(liftedFootPos, pos);
            var lerp = distToPos / maxDist;
            if (lerp > 1f)
            {
                lerp = 1f;
            }
            
            var maxDirection = dirToPos.normalized * Context.SneakStepLength;
            var lerpedDirection = Vector3.Lerp(dirToPos, maxDirection, Context.SpeedCurve.Evaluate(lerp));
            liftedDirection = lerpedDirection;
        }
        
        // Modify the movement settings
        var newLiftedMovementSettings = Context.LiftedMovementSettings;
        newLiftedMovementSettings.MaxSpeed += Context.SneakSpeed;
        newLiftedMovementSettings.Acceleration += Mathf.Pow(Context.SneakSpeed, Context.SneakSpeed);
        
        // Move the feet to their grounded positions
        _sPlantedFootGoalVel = MoveRigidbody(Context.PlantedFoot, plantedDirection, _sPlantedFootGoalVel, Context.PlantedMovementSettings);
        _sLiftedFootGoalVel = MoveRigidbody(Context.LiftedFoot, liftedDirection, _sLiftedFootGoalVel, newLiftedMovementSettings);
    }
}