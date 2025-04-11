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
        {
            // *** TEMP ***
            // Check if it is valid to exit
            // Check if foot has ground
            if (!Context.FootGroundCast(1.5f) && _validExit)
            {
                _validExit = false;
            }
            
            if (_validExit)
                return FootControlStateMachine.EFootState.Placing;
            
            // if close to the last foot position, we want to place it
            var posToGoto = Context.LastFootPosition;
            var footPos = Context.Foot.Target.position;
            
            posToGoto.y = 0f;
            footPos.y = 0f;
            var dist = Vector3.Distance(footPos, posToGoto);
            if (dist < 0.015f)
            {
                return FootControlStateMachine.EFootState.Placing;
            }
        }
        
        if (GetDistanceFromOtherFoot() > Context.StepLength && Context.BothInputsPressed)
            return FootControlStateMachine.EFootState.Placing;
        
        return StateKey;
    }
    
    private bool _validExit;
    private Collider _storedCollider;
    
    public override void EnterState()
    {
        // get the start angle
        _startAngle = Context.Foot.Target.transform.localRotation.x;
        Context.LastFootPosition = Context.FootGroundCast(out var hit) ? hit.point : Context.Foot.Target.position;
        _validExit = true;
        _storedCollider = hit.collider;
    }

    public override void ExitState()
    {
        _liftTimer = 0f;
        Context.FootSoundPlayer.MakeFootSound(PlayerFootSoundPlayer.EFootSoundType.Wood);
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
        
        // *** TEMP ***
        if(!_validExit)
            input = Vector3.zero;
        
        bool useLastFootPos = input == Vector3.zero && Context.LastFootPosition != Vector3.zero;
        if (useLastFootPos)
            if(Context.FootGroundCast(out var hit) && hit.collider == _storedCollider)
                useLastFootPos = false;
        
        var baseFootLiftedHeight = 0.15f;
        var wantedHeight = otherFootPos.y + baseFootLiftedHeight;
        
        // Depending on obstacles, we want to move the foot up
        // We create a boxcast from the max step height, downwards to the max step height
        // If we don't hit anything, the pressed height is the wanted height
        if (ScanGroundObject(input, out var heightOfObject) && !useLastFootPos)
            wantedHeight = heightOfObject + baseFootLiftedHeight;

        if (useLastFootPos)
            wantedHeight = Context.LastFootPosition.y + baseFootLiftedHeight;
        
        // This is the current position of the foot, but at the wanted height
        var wantedHeightPos = new Vector3(footPos.x, wantedHeight, footPos.z);
        
        if(useLastFootPos)
            wantedHeightPos = new Vector3(Context.LastFootPosition.x, wantedHeight, Context.LastFootPosition.z);
        
        // We also calculate the input position based on the player input
        // But also take into account the wanted height
        var wantedInputPos = wantedHeightPos + input.normalized;
        
        if(useLastFootPos)
            wantedInputPos = new Vector3(Context.LastFootPosition.x, wantedHeight, Context.LastFootPosition.z);
        
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
        
        // Update dir to pos because we might have changed the position
        dirToPos = pos - footPos;
        
        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dirToPos.magnitude;
        var breakDistance = 0.05f;
        var magLerp = mag / breakDistance;
        dirToPos.Normalize();
        dirToPos *= Context.SpeedCurve.Evaluate(magLerp);
        
        // *** TEMP ***
        if (dirToPos.magnitude < 0.02f && !_validExit)
        {
            _validExit = true;
        }
        
        Context.MoveFootToPosition(dirToPos);
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
        var forwardCheck = size.z * 3f;
        // How much we should check side to side
        var sideCheck = 0.1f;
        
        // Calculate the extensions
        var forward = Vector3.zero;
        if (input != Vector3.zero)
        {
            forward = input.normalized * forwardCheck;
            // Also set the rotation to the input direction
            rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            // Add extensions
            position += forward;
            size.x = sideCheck;
            size.z = forwardCheck / 2f;
        }
        
        // Height 
        var footSize = size.y;
        var maxHeight = Context.StepHeight + Context.OtherFoot.Target.position.y;
        position.y = maxHeight + size.y;
        
        // Cast
        if (!Physics.BoxCast(position, size, Vector3.down, out var hit, rotation, (Context.StepHeight * 2f) + size.y,
                Context.GroundLayers))
            return false;
        height = hit.point.y;
        var closestPoint = hit.collider.ClosestPoint(footPos);
        
        // Check if it is too high
        if (CalculateIntersectionPoint(Context.OtherFoot.Target.position, Context.StepLength,
                closestPoint + Vector3.down, Vector3.up, out var highestPoint))
        {
            var highest = highestPoint.y - footSize;
            // If there is space above, we can return true
            if (highest > height)
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
                
                // *** TODO ***
                // FIGURE OUT WHAT HAPPENED HERE
                // If we hit it, we set the height to the hit point - foot size
                // -0.15f is to nullify the lifting default height
                if(Physics.BoxCast(position, size, Vector3.up, out var upHit, rotation,
                       dist + size.y,
                       Context.GroundLayers))
                    height = otherFootPos.y;
                else
                {
                    // If this does not hit, and we are not moving,
                    // Store the last foot position to the hit point
                    if(input != Vector3.zero)
                        TryForPosition(hit.point, hit.collider);
                }
                return true;
            }
            
            // Check if we are actually on the ground
            if (Context.IsFootGrounded)
            {
                // If we are not moving,
                // Store the last foot position to the hit point
                if(input != Vector3.zero)
                    TryForPosition(hit.point, hit.collider);
                return true;
            }
            
            // If it's too high, we want to check a smaller box
            position = defaultValues.Position;
            size = defaultValues.Size;
            rotation = defaultValues.Rotation;
            
            position.y = hit.point.y;
            
            // Smaller cast to see if we get better results
            if (Physics.BoxCast(position, size, Vector3.down, out var heightHit, rotation,
                    (Context.StepHeight * 2f) + size.y,
                    Context.GroundLayers))
            {
                var highClosestPoint = heightHit.collider.ClosestPoint(footPos);
                // Check if the new height is reachable
                if (CalculateIntersectionPoint(Context.OtherFoot.Target.position, Context.StepLength,
                        highClosestPoint + Vector3.down, Vector3.up, out highestPoint))
                {
                    height = heightHit.point.y;
                    highest = highestPoint.y - footSize;
            
                    // If it has space, we can return true
                    if (highest > height)
                    {
                        // If we are not moving,
                        // Store the last foot position to the hit point
                        if(input != Vector3.zero)
                            TryForPosition(heightHit.point, heightHit.collider);
                        return true;
                    }
                }
            } 
            
            // If we don't hit anything, no ground
        }        

        /*
        // *** NOT REALLY DOING ANYTHING ***
        // Check if it is too low
        if (!CalculateIntersectionPoint(Context.OtherFoot.Target.position, Context.StepLength,
                closestPoint + Vector3.up, Vector3.down, out var lowestPoint))
            return false; // out of bounds
        
        var lowest = lowestPoint.y + footSize;
        // If it is high enough, we can return true
        if (height > lowest)
        {
            // We also dont want to set the height to the lowest point
            // We want to set it to a fixed height
            height = Context.OtherFoot.Target.position.y - 0.15f;
            // But clamp it so that it doesn't go below the lowest point
            if (height < lowest)
                height = lowest;
            return true;
        }
        */

        return false; // Out of bounds
    }

    private void TryForPosition(Vector3 pos, Collider hitCollider)
    {

        if (!_storedCollider)
        {
            _storedCollider = hitCollider;
            Context.LastFootPosition = pos;
            
            return;
        }

        if (_storedCollider == hitCollider)
        {
            Context.LastFootPosition = pos;
            return;
        }

        if (_storedCollider != hitCollider)
        {
            _storedCollider = hitCollider;
            Context.LastFootPosition = pos;
            return;
        }
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
        
        // Dir from other foot to pos
        var otherDir = (direction + footPos) - otherFootPos;
            
        // We check the other intersection point now, in case the normal one fails, this one will never fail
        CalculateIntersectionPoint(otherFootPos, Context.StepLength, otherFootPos, otherDir.normalized,
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