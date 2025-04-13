using UnityEngine;
using static CircleLineIntersection;

public class FootLiftedState : FootControlState
{
    public FootLiftedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if (!Context.IsFootLifting)
            return FootControlStateMachine.EFootState.Placing;
        
        if (GetDistanceFromOtherFoot() > Context.StepLength && Context.BothInputsPressed)
            return FootControlStateMachine.EFootState.Placing;
        
        return StateKey;
    }

    private Vector3 _storedPos;
    
    public override void EnterState()
    {
        // get the start angle
        _startAngle = Context.Foot.Target.transform.localRotation.x;
    }

    public override void ExitState()
    {
        _liftTimer = 0f;
        Context.FootSoundPlayer.MakeFootSound(PlayerFootSoundPlayer.EFootSoundType.Wood);
        
        // Set the xz velocity to 0
        Context.Foot.Target.linearVelocity = new Vector3(0f, Context.Foot.Target.linearVelocity.y, 0f);
    }

    public override void UpdateState()
    {
        _liftTimer += Time.deltaTime;
    }

    public override void FixedUpdateState()
    {
        var footPos = Context.Foot.Target.position;
        var otherFootPos = Context.OtherFoot.Target.position;
        var input = Context.Player.RelativeMoveInput;
        
        var baseFootLiftedHeight = 0.15f;
        var wantedHeight = otherFootPos.y + baseFootLiftedHeight;
        
        // Depending on obstacles, we want to move the foot up
        // We create a boxcast from the max step height, downwards to the max step height
        // If we don't hit anything, the pressed height is the wanted height
        if (ScanGroundObject(input, out var heightOfObject))
            wantedHeight = heightOfObject + baseFootLiftedHeight;
        
        // This is the current position of the foot, but at the wanted height
        var wantedHeightPos = new Vector3(footPos.x, wantedHeight, footPos.z);
        
        // We also calculate the input position based on the player input
        // But also take into account the wanted height
        var wantedInputPos = wantedHeightPos + input.normalized;
        
        // *** TEMP ***
        // If the foot is behind the other foot,
        // We want to move this foot towards an offset to the side of the other foot
        // The position to the side of the other foot
        var offsetPos = GetOffsetPosition(otherFootPos, wantedHeight, 0.2f);
        
        // Depending on down angle, we dont care about offset
        var camAngle = Context.Player.Camera.CameraX;
        var offsetLerp = camAngle / 60f;
        offsetPos = Vector3.Lerp(offsetPos, wantedInputPos, offsetLerp);

        
        // We lerp our input position with the offset position based on how far behind the foot is
        var distBehind = Context.RelativeDistanceInDirection(footPos, otherFootPos, input.normalized);
        var wantedPos = Vector3.Lerp(wantedInputPos, offsetPos, Context.OffsetCurve.Evaluate(distBehind/Context.StepLength));
        
        // Depending on our distance to the wanted height compared to our current height
        // We lerp what position we want to go to
        var currentHeight = footPos.y - otherFootPos.y;
        var maxHeight = wantedHeight - otherFootPos.y;
        // Doesn't start to lerp until we are 50% of the way to the max height
        var posLerp = currentHeight / maxHeight;
        var pos = Vector3.Lerp(wantedHeightPos, wantedPos, Context.HeightCurve.Evaluate(posLerp));
        
        // *** TEMP ***
        // If the wanted height is downwards, we dont care to lerp
        if(currentHeight > maxHeight)
            pos = wantedPos;
        
        // We get the direction towards the wanted position
        var dirToPos = pos - footPos;

        // Now clamp if we are going out of step length
        if (Vector3.Distance(pos, otherFootPos) > Context.StepLength)
            pos = ClampedFootPosition(footPos, otherFootPos, dirToPos);
        /*
        // Update dir to pos because we might have changed the position
        dirToPos = pos - footPos;
        
        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dirToPos.magnitude;
        var breakDistance = 0.05f;
        var magLerp = mag / breakDistance;
        dirToPos.Normalize();
        dirToPos *= Context.SpeedCurve.Evaluate(magLerp);
        
        //Context.MoveFootToPosition(dirToPos);
        var settings = Context.MovementSettings;
        if (input == Vector3.zero)
        {
            var settingsMaxSpeed = Mathf.Lerp(0.1f, 0.2f, mag);
            settings.MaxSpeed *= settingsMaxSpeed;
        }
        */

        RigidbodyMovement.MoveToRigidbody(Context.Foot.Target, pos, Context.MovementSettings);

        HandleFootRotation();
    }
    
    private bool ScanGroundObject(Vector3 input, out float height)
    {
        var footPos = Context.Foot.Target.position;
        var otherFootPos = Context.OtherFoot.Target.position;
        height = 0f;
        var defaultValues = Context.FootCastValues;
        
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        
        // How much we should check forwards
        var forwardCheck = size.z * 1.5f;
        // How much we should check side to side
        var sideCheck = 0.1f;
        
        // Calculate the extensions
        var forward = Vector3.zero;
        if (input != Vector3.zero)
        {
            forward = input.normalized * forwardCheck / 2f;
            // Also set the rotation to the input direction
            rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            // Add extensions
            position += forward;
            size.x = sideCheck;
            size.z = forwardCheck;
        }
        
        // Height 
        var footSize = size.y;
        var maxHeight = Context.StepHeight + otherFootPos.y;
        
        // Clamp the y height to the foot height + the step height
        // Avoid too high checks
        position.y = maxHeight + size.y;
        position.y = Mathf.Clamp(position.y, otherFootPos.y - Context.StepHeight, footPos.y + Context.StepHeight);
        
        // Cast
        if (!Physics.BoxCast(position, size, Vector3.down, out var hit, rotation, (Context.StepHeight * 2f) + size.y,
                Context.GroundLayers))
            return false;
        height = hit.point.y;
        var closestPoint = hit.collider.ClosestPoint(footPos);
        
        // Check if it is too high - If it is too far away, the highest point will be 0
        CalculateIntersectionPoint(otherFootPos, Context.StepLength,
            closestPoint + Vector3.down, Vector3.up, out var highestPoint);
        
        // Check if it is too far away
        var tooFar = Vector3.Distance(otherFootPos, closestPoint) > Context.StepLength;
        Physics.CheckSphere(closestPoint, 0.02f);
        Physics.CheckSphere(otherFootPos + ((closestPoint - otherFootPos).normalized * Context.StepLength), 0.02f);
        
        var highest = highestPoint.y - footSize;
        // If there is space above and the point is not too far away
        // We want to continue
        if (highest > height && !tooFar)
        {
            // If the thing we are trying to step on is lower than the other foot,
            // We don't want to put our foot directly on it
            // So we limit how far down we can go
            if(otherFootPos.y -0.20f > height)
                height = otherFootPos.y - 0.20f;
                
            // Do a check to see if there is anything above
            position = defaultValues.Position;
            size = defaultValues.Size;
            rotation = defaultValues.Rotation;
            position.y -= size.y;
            var dist = height - position.y;
                
            // If we hit it, we set the height to the hit point - foot size
            // -0.15f is to nullify the lifting default height
            if(Physics.BoxCast(position, size, Vector3.up, out var upHit, rotation,
                   dist + size.y,
                   Context.GroundLayers))
                height = otherFootPos.y;
                
            Context.SetSafePosition(hit.point);
            return true;
        }
            
        // Check if we are actually on the ground
        if (Context.IsFootGrounded)
        {
            Context.SetSafePosition(hit.point);
            return true;
        }
            
        // If it's too high or too far away, we want to check a smaller box
        position = defaultValues.Position;
        size = defaultValues.Size;
        rotation = defaultValues.Rotation;
            
        position.y = hit.point.y;
            
        // Smaller cast to see if we get better results
        if (!Physics.BoxCast(position, size, Vector3.down, out var heightHit, rotation,
                (Context.StepHeight * 2f) + size.y,
                Context.GroundLayers))
            return false;
            
        var highClosestPoint = heightHit.collider.ClosestPoint(footPos);
        // Check if the new height is reachable
        if (!CalculateIntersectionPoint(otherFootPos, Context.StepLength,
                highClosestPoint + Vector3.down, Vector3.up, out highestPoint))
            return false;
            
        height = heightHit.point.y;
        highest = highestPoint.y - footSize;
            
        // If it has space, we check if it is too low before returning true
        if (highest < height)
            return false;
            
        if (!CalculateIntersectionPoint(otherFootPos, Context.StepLength,
                highClosestPoint + Vector3.up, Vector3.down, out highestPoint))
            return false;
            
        var lowest = highestPoint.y + footSize;
        if (lowest > height)
            return false;
            
        Context.SetSafePosition(heightHit.point);
        return true;
    }

    private Vector3 GetOffsetPosition(Vector3 otherFootPos, float wantedHeight, float dist)
    {
        // Maybe implement a dot product to see if we should offset forwards or backwards also?
        
        var right = -Vector3.Cross(Context.Player.RelativeMoveInput.normalized, Vector3.up) * dist;
        
        // If the foot is on the left side, we want it to move to the left
        if(Context.Foot.Side == Foot.EFootSide.Left)
            right = -right;
        
        // If walking backwards, we want to move the foot to the other side
        var dot = Vector3.Dot(Context.Player.Camera.GetCameraYawTransform().forward.normalized, Context.Player.RelativeMoveInput.normalized);
        if (dot < -0.2f)
            right = -right;
        
        return new Vector3(otherFootPos.x, wantedHeight, otherFootPos.z) + right;
    }
    
    private Vector3 ClampedFootPosition(Vector3 footPos, Vector3 otherFootPos, Vector3 direction)
    {
        // Offset the start position of the line to avoid not finding an intersection point when we should
        var lineStart = footPos - direction.normalized;
        
        // We dont want the otherFootIntersection to do anything on the y axis
        // So we set the other foot pos to the same y as the foot
        var footHeight = footPos.y;
        var otherPosSim = new Vector3(otherFootPos.x, footHeight, otherFootPos.z);
        // We also cant let the simulation go above or below step StepLength
        var maxHeight = otherFootPos.y + Context.StepLength - 0.01f;
        var minHeight = otherFootPos.y - Context.StepLength + 0.01f;
        if (otherPosSim.y > maxHeight)
            otherPosSim.y = maxHeight;
        else if (otherPosSim.y < minHeight)
            otherPosSim.y = minHeight;
        
        // Dir from other foot to pos
        var otherDir = (direction + footPos) - otherPosSim;
            
        // We check the other intersection point now, in case the normal one fails, this one will never fail
        CalculateIntersectionPoint(otherFootPos, Context.StepLength, otherPosSim, otherDir.normalized,
            out var otherFootIntersect);
        
        // If we don't fail, we should use this intersection point for more accurate movement
        // Else we use the other foot intersection point
        if (!CalculateIntersectionPoint(otherFootPos, Context.StepLength, lineStart, direction.normalized,
                out var footIntersect))
            return otherFootIntersect;
        
        // Only start lerping when we are close enough to the edge && when input is pressed
        if(InputManager.Instance.MoveInput == Vector3.zero)
            return footIntersect;
        
        var startDist = Context.StepLength * 0.1f;
        var distToIntersect = Vector3.Distance(footPos, footIntersect);
        var lerp = distToIntersect / startDist;
        
        // The closer we are to the edge, the more we want to use the other foot intersection point
        return Vector3.Lerp(otherFootIntersect, footIntersect, lerp);
    }

    private Vector3 _storedInput;
    private float _dot;
    private float _storedCamAngle;
    private Vector3 _forward;
    private Vector3 _right;
    private float _liftTimer;
    private float _startAngle;
    private void HandleFootRotation()
    {
        var isMoving = InputManager.Instance.MoveInput.magnitude > 0.01f;
        if (isMoving)
        {
            // *** TODO ***
            // We need to change the speed it rotates, or clamp the max so it doesn't rotate too fast with small movements
            _storedInput = Vector3.MoveTowards(_storedInput, Context.Player.RelativeMoveInput.normalized, Time.fixedDeltaTime * 2.5f);
            _dot = Vector3.Dot(Context.Player.Camera.GetCameraYawTransform().forward.normalized, _storedInput.normalized);
            _storedCamAngle= Context.Player.Camera.CameraX;
        }

        var downLerp = _storedCamAngle / 60f;
        
        // Store the camera forward direction if we are moving
        if (isMoving)
        {
            // Other direction
            if (_dot < -0.2f)
                _storedInput = -_storedInput;
            
            // Makes the foot turn it slightly diagonally if it should
            var cameraForward = Context.Player.Camera.GetCameraYawTransform().forward.normalized;
            var footAngleDot1 = Mathf.Clamp01(_dot);
            var dif1 = 1 - footAngleDot1;
            var maxDif1 = 0.8f;
            footAngleDot1 = dif1 / maxDif1;
            // Only want to change if the camera angle is right
            footAngleDot1 = Mathf.Lerp(1,footAngleDot1, downLerp);
            
            // We change the forward if the angle is right
            var lerpFootForward = Vector3.Lerp(_storedInput, cameraForward, footAngleDot1);
            
            _forward = Vector3.Lerp(_storedInput, lerpFootForward, downLerp);
            _right = Vector3.Lerp(-Vector3.Cross(_storedInput, Vector3.up), Context.Player.Camera.GetCameraYawTransform().right.normalized, downLerp);
        }
        
        // Update the lifted foot pitch
        var minPitch = 85f;
        var maxPitch = -85f;
        
        var minRelDist = -Context.StepLength;
        var maxRelDist = Context.StepLength;
        var relDist = Context.RelativeDistanceInDirection(Context.OtherFoot.Target.position, Context.Foot.Target.position, _forward);
        
        // We also want to include the dot, if we are stepping diagonally, we dont want to rotate the foot on x
        var footAngleDot = Mathf.Clamp01(_dot);
        var dif = 1 - footAngleDot;
        var maxDif = 0.8f;
        footAngleDot = dif / maxDif;
        footAngleDot = Mathf.Lerp(1, 0, footAngleDot);
        // Only want to change if the camera angle is right
        footAngleDot = Mathf.Lerp(1,footAngleDot, downLerp);

        // If the angle is too high, we dont want it to rotate on x
        relDist *= footAngleDot;
        
        // Lerp Foot Pitch
        var angle = Mathf.Lerp(minPitch, maxPitch, Mathf.InverseLerp(minRelDist, maxRelDist, relDist));
        var lerpAngle = Mathf.Lerp(_startAngle, angle, Context.PlaceCurve.Evaluate(_liftTimer / 0.20f));
        
        // Rotate the camForward direction around the foot's right direction
        var footForward = Quaternion.AngleAxis(lerpAngle, _right) * _forward;
        footForward.Normalize();

        RigidbodyMovement.RotateRigidbody(Context.Foot.Target, footForward, 350f);
    }
    
    private float GetDistanceFromOtherFoot()
    {
        var footPos = Context.Foot.Target.position;
        var otherFootPos = Context.OtherFoot.Target.position;

        footPos.y = 0f;
        otherFootPos.y = 0f;
        
        var dist = Context.RelativeDistanceInDirection(otherFootPos, footPos, Context.Player.RelativeMoveInput.normalized);
        
        return dist;
    }
}