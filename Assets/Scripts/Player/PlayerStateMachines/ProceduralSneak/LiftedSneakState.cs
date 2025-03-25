using Unity.VisualScripting;
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
        if (!Context.Player.IsGrounded)
        {
            return ProceduralSneakStateMachine.ESneakState.Stop;
        }
        
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
        Context.ResetBodyGoalVel();
        ResetFootsVelocities();
        
        // get the start angle
        _startAngle = Context.LiftedFoot.transform.localRotation.x;
    }

    public override void ExitState()
    {
        _liftTimer = 0f;
    }

    public override void UpdateState()
    {
        _liftTimer += Time.deltaTime;
    }

    public override void FixedUpdateState()
    {
        MoveFeet();
        HandleFootRotation();
        
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
    
    private Vector3 _sCameraForward;
    private Vector3 _sCameraRight;
    private float _liftTimer;
    private float _startAngle;
    private void HandleFootRotation()
    {
        var isMoving = InputManager.Instance.MoveInput.magnitude > 0.01f;
        
        // Store the camera forward direction if we are moving
        if (isMoving)
        {
            _sCameraForward = Context.Player.Camera.GetCameraYawTransform().forward;
            _sCameraRight = Context.Player.Camera.GetCameraYawTransform().right;
        }
        
        // Update the lifted foot pitch
        var minPitch = 85f;
        var maxPitch = -85f;
        
        var minRelDist = -Context.SneakStepLength;
        var maxRelDist = Context.SneakStepLength;
        var relDist = RelativeDistanceInDirection(Context.PlantedFoot.position, Context.LiftedFoot.position, Context.Player.Camera.GetCameraYawTransform().forward);
        
        // Lerp Foot Pitch
        var angle = Mathf.Lerp(minPitch, maxPitch, Mathf.InverseLerp(minRelDist, maxRelDist, relDist));
        var lerpAngle = Mathf.Lerp(_startAngle, angle, Context.PlaceSpeedCurve.Evaluate(_liftTimer / 0.20f));
        
        
        // Rotate the camForward direction around the foot's right direction
        var footForward = Quaternion.AngleAxis(lerpAngle, _sCameraRight) * _sCameraForward;
        footForward.Normalize();

        if(footForward == Vector3.zero)
            return;
        
        RotateRigidbody(Context.LiftedFoot, footForward, 500f);
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
        }
        // Update the planted foot ground normal no matter what
        Context.UpdateFootGroundNormal(Context.PlantedFoot);
        

        
        // Get both foot positions
        var liftedFootPos = Context.LiftedFoot.position;
        liftedFootPos.y = 0;
        var plantedFootPos = Context.PlantedFoot.position;
        plantedFootPos.y = 0;
        
        // Height calculation
        var isLifting = InputManager.Instance.IsLifting;
        var baseHeight = Context.PlantedFoot.position.y + Context.FootPlaceOffset;

        if (!isLifting)
        {
            // If we are not lifting:
            // The further the foot is from the planted foot, the more the ground height matters
            var footDist = Vector3.Distance(liftedFootPos, plantedFootPos);
            // This distance should not be greater than the step length
            // We use this to lerp between planted pos and the ground pos
            var groundHeight = Context.PlantedFoot.position.y;
            if(Physics.Raycast(Context.LiftedFoot.position, Vector3.down, out var hit,
                   Context.SneakStepHeight, Context.GroundLayers))
                groundHeight = hit.point.y;
            baseHeight = Mathf.Lerp(Context.PlantedFoot.position.y, groundHeight, footDist/Context.SneakStepLength);
        }
        
        // Add y value
        var addedLiftHeight = 0.05f;
        if (isLifting)
        {
            addedLiftHeight += 0.15f;
        }
        
        // Ground cast to see if foot is too close to the ground
        if(Physics.CheckSphere(liftedFootPos, 0.01f, Context.GroundLayers))
            addedLiftHeight += PlayerController.HeightThreshold;
        
        // Combine the height values
        float height = baseHeight + addedLiftHeight;
        
        // Clamp the height to not go above or below the step height relative to the planted foot
        var plantedFootHeight = Context.PlantedFoot.position.y + Context.FootPlaceOffset;
        height = Mathf.Clamp(height, plantedFootHeight - Context.SneakStepHeight, plantedFootHeight + Context.SneakStepHeight);
        
        // Project the lifted direction onto the ground normal
        var cast = Context.GroundCast(Context.LiftedFoot.position, height + Context.FootPlaceOffset * 1.5f);
        var groundNormal = cast.normal;
        liftedDirection = Vector3.ProjectOnPlane(liftedDirection, groundNormal);
        
        // Get the wanted position of the lifted foot after the step
        var footDir = liftedFootPos - plantedFootPos;
        var pos = liftedFootPos + liftedDirection;
        
        // Add the height to the walk pos
        pos += Vector3.up * height;
        // Clamp the position to not go above or below the step height relative to the planted foot
        pos.y = Mathf.Clamp(pos.y, plantedFootHeight - Context.SneakStepHeight, plantedFootHeight + Context.SneakStepHeight);
        
        // Update the lifted foot direction
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
    
    private float RelativeDistanceInDirection(Vector3 from, Vector3 to, Vector3 direction)
    {
        // Get the direction from the from position to the to position
        var dir = to - from;
        
        // Get the dot product of the direction and the dir
        var dot = Vector3.Dot(dir.normalized, direction.normalized);
        
        // Multiply the direction magnitude by the dot product
        return dir.magnitude * dot;
    }
}