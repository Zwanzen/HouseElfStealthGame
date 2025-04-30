using Unity.VisualScripting;
using UnityEngine;
using static CircleLineIntersection;
using static RigidbodyMovement;
using Pathfinding;
using Unity.Collections;

public class FootLiftedState : FootControlState
{
    public FootLiftedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        // If we let go, and not auto waliking, we can try stop as long as the other foot is planted.
        // If not, we are in a leaping state, this is detected by the player control sm.
        if (!Context.IsFootLifting && Context.Player.IsSneaking && Context.OtherFoot.Planted)
            return FootControlStateMachine.EFootState.Placing;

        // Used for auto walk rn
        if (GetDistanceFromOtherFoot() > Context.StepLength * 0.45f && !Context.Player.IsSneaking)
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
    }

    public override void UpdateState()
    {
        // If we jumped, and this foot is lifted,
        // when we press the relating mbutton, we want to stop jumping.
        // This is because the player has to take control of the foot again.
        if (Context.IsMBPressed)
            Context.Player.SetJump(false);

        _liftTimer += Time.deltaTime;
    }

    private float _timerSinceInput;
    public override void FixedUpdateState()
    {
        // If the other foot is placing, we want to keep this foot close to the other foot
        if (Context.OtherFoot.Placing)
        {
            // If we are in close enough to the other foot, we want to be able to move around again.
            var velDir = Context.OtherFoot.Target.linearVelocity.normalized;
            velDir.y = 0f;
            if (velDir == Vector3.zero)
                velDir = Context.Player.Camera.GetCameraYawTransform().forward;
            var dist = Context.RelativeDistanceInDirection(Context.OtherFoot.Position, Context.Foot.Position, velDir);
            var offsetPos = GetOffsetPosition(Context.Player.Camera.GetCameraYawTransform().forward, Context.OtherFoot.Position.y, 0.2f);
            MoveToRigidbody(Context.Foot.Target, offsetPos, Context.CurrentWalkSettings, Context.OtherFoot.Target.linearVelocity);
        }
        // If not, we use normal movement
        else
        {
            Movement();
        }

        HandleFootRotation();
    }



    private void Movement()
    {
        var footPos = Context.Foot.Position;
        var otherFootPos = Context.OtherFoot.Position;
        var input = Context.Player.RelativeMoveInput;

        var baseFootLiftedHeight = 0.1f;
        var wantedHeight = otherFootPos.y + baseFootLiftedHeight;

        /*
        // Depending on obstacles, we want to move the foot up
        // We create a boxcast from the max step height, downwards to the max step height
        // If we don't hit anything, the pressed height is the wanted height
        if (ScanGroundObject(input, out var scanInfo) && !Input.GetKey(KeyCode.LeftShift))
            wantedHeight = scanInfo.Height + baseFootLiftedHeight;
        */
        if (TryWantedHeightPos(input, footPos, otherFootPos, out var r))
        {
            Physics.CheckSphere(r, 0.1f, 0);
            if (r.y < otherFootPos.y)
                r.y = otherFootPos.y - 0.15f;
            wantedHeight = r.y + baseFootLiftedHeight;
        }

        // This is the current position of the foot, but at the wanted height
        var wantedHeightPos = new Vector3(footPos.x, wantedHeight, footPos.z);

        // We also calculate the input position based on the player input
        // But also take into account the wanted height
        var wantedInputPos = wantedHeightPos + input.normalized;

        // *** TEMP ***
        // If the foot is behind the other foot,
        // We want to move this foot towards an offset to the side of the other foot
        // The position to the side of the other foot
        var offsetPos = GetOffsetPosition(input, wantedHeight, 0.2f);

        // Depending on down angle, we dont care about offset
        var camAngle = Context.Player.Camera.CameraX;
        var offsetLerp = camAngle / 60f;
        offsetPos = Vector3.Lerp(offsetPos, wantedInputPos, offsetLerp);

        // We lerp our input position with the offset position based on how far behind the foot is
        var distBehind = Context.RelativeDistanceInDirection(footPos, otherFootPos, input.normalized);
        var wantedPos = Vector3.Lerp(wantedInputPos, offsetPos, Context.OffsetCurve.Evaluate(distBehind / Context.StepLength));

        // Depending on our distance to the wanted height compared to our current height
        // We lerp what position we want to go to
        var currentHeight = footPos.y - otherFootPos.y;
        var maxHeight = wantedHeight - otherFootPos.y;
        // Doesn't start to lerp until we are 50% of the way to the max height
        var posLerp = currentHeight / maxHeight;
        var pos = Vector3.Lerp(wantedHeightPos, wantedPos, Context.HeightCurve.Evaluate(posLerp));

        // *** TEMP ***
        // If the wanted height is downwards, we dont care to lerp
        if (currentHeight > maxHeight)
            pos = wantedPos;

        // We get the direction towards the wanted position
        var dirToPos = pos - footPos;

        // Now clamp if we are going out of step length
        if (Vector3.Distance(pos, otherFootPos) > Context.StepLength)
            pos = ClampedFootPosition(footPos, otherFootPos, dirToPos);

        var settings = Context.Player.IsSneaking ? Context.CurrentSneakSettings : Context.CurrentWalkSettings;
        MoveToRigidbody(Context.Foot.Target, pos, settings);

    }

    private struct ScanInfo
    {
        public readonly float Height;
        public readonly bool Stuck;

        public ScanInfo(float height, bool stuck)
        {
            Height = height;
            Stuck = stuck;
        }
    }

    private RaycastHit[] _hits = new RaycastHit[20];

    /// <summary>
    /// Calculates the target height position we want to go to depending on the input.
    /// </summary>
    private bool TryWantedHeightPos(Vector3 input, Vector3 footPos, Vector3 otherPos, out Vector3 heightPos)
    {
        heightPos = Vector3.zero;

        var radius = Context.StepLength * 0.5f;
        var castHeight = otherPos.y + Context.StepHeight + radius;
        var castPos = new Vector3(footPos.x, castHeight, footPos.z);
        // We want to offset the cast position in the direction of the input
        castPos += input.normalized * 0.2f;
        var dist = Context.StepHeight * 2f + radius;

        var hitCount = Physics.SphereCastNonAlloc(castPos, radius, Vector3.down, _hits, dist, Context.GroundLayers);
        if (hitCount <= 0)
            return false;

        // We need to efficiently store the reachable points
        // Create with maximum possible size (hitCount)
        var hitPoints = new NativeArray<RaycastHit>(hitCount, Allocator.Temp);
        int validPointCount = 0;
        for (var i = 0; i < hitCount; i++)
        {
            if (IsReachablePoint(_hits[i].point, footPos, otherPos, input))
            {
                Debug.DrawRay(_hits[i].point, Vector3.up * 0.1f, Color.green);
                // If the point is reachable, we want to add it to the list
                hitPoints[validPointCount] = _hits[i];
                validPointCount++;
            }
        }

        // If there is no valid point, we want to return false
        if (validPointCount == 0)
        {
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return false;
        }

        // If there is only one point, we want to use that point
        // But only if we have pressed the input
        if (validPointCount == 1 && input != Vector3.zero)
        {
            heightPos = hitPoints[0].point;
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return true;
        }

        // If we have more than 1 point,
        // And we there is no input, we want to get the closest point
        // Used multiple times, so we store it
        var footXZPos = new Vector3(footPos.x, 0f, footPos.z);
        var closestPoint = Vector3.zero;
        var xzPoint = Vector3.zero;
        if (input == Vector3.zero)
        {
            for (var i = 0; i < validPointCount; i++)
            {
                // If it has no point, we want to use the first point
                if (closestPoint == Vector3.zero)
                {
                    closestPoint = hitPoints[i].point;
                    continue;
                }
                // Check if the point is closer than the closest point
                if (Vector3.Distance(hitPoints[i].point, footPos) < Vector3.Distance(closestPoint, footPos))
                    closestPoint = hitPoints[i].point;
            }

            // Only if it is close enough to the foot, we want to use it
            xzPoint = closestPoint;
            xzPoint.y = 0f;
            if (Vector3.Distance(xzPoint, footXZPos) < 0.2f)
            {
                heightPos = closestPoint;
                // Always dispose NativeArray when done
                hitPoints.Dispose();
                return true;
            }
        }

        // If there are multiple points, we check if there are points above the foot
        var hasAbove = false;
        for (var i = 0; i < validPointCount; i++)
        {
            // Check if the point is above the foot
            if (hitPoints[i].point.y > footPos.y)
            {
                hasAbove = true;
                break;
            }
        }

        if (hasAbove)
        {
            // If we have points above,
            // we want to use the point closest to the foot on the xz plane
            // but if they are below, and they are close, we skip them
            var closestXZPoint = Vector3.zero;
            for (var i = 0; i < validPointCount; i++)
            {
                var hitXZPos = new Vector3(hitPoints[i].point.x, 0f, hitPoints[i].point.z);
                var d = Vector3.Distance(hitXZPos, footXZPos);
                if (hitPoints[i].point.y < footPos.y)
                    if (d < 0.25f)
                        continue;
                // If it has no point, we want to use the first point
                if (closestXZPoint == Vector3.zero)
                {
                    closestXZPoint = hitPoints[i].point;
                    continue;
                }

                // Check if the point is closer than the closest point
                if (d < Vector3.Distance(closestXZPoint, footXZPos))
                    closestXZPoint = hitPoints[i].point;
            }
            // We want to use the point closest to the foot on the xz plane
            heightPos = closestXZPoint;
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return true;
        }

        // If nothing is above, we want to use the point closest to the foot
        for (var i = 0; i < validPointCount; i++)
        {
            // If it has no point, we want to use the first point
            if (closestPoint == Vector3.zero)
            {
                closestPoint = hitPoints[i].point;
                continue;
            }
            // Check if the point is closer than the closest point
            if (Vector3.Distance(hitPoints[i].point, footPos) < Vector3.Distance(closestPoint, footPos))
                closestPoint = hitPoints[i].point;
        }

        // Only if it is close enough to the foot, we want to use it
        xzPoint = closestPoint;
        xzPoint.y = 0f;
        if (Vector3.Distance(xzPoint, footXZPos) < 0.2f)
        {
            heightPos = closestPoint;
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return true;
        }

        // Always dispose NativeArray when done
        hitPoints.Dispose();
        return false;
    }

    private bool IsReachablePoint(Vector3 point,Vector3 footPos, Vector3 otherPos, Vector3 input)
    {
        // Check if it is too far way
        if(Vector3.Distance(otherPos, point) > Context.StepLength)
            return false;

        var maxHeight = Context.StepHeight + otherPos.y - 0.035;
        var minHeight = otherPos.y - Context.StepHeight;

        // Check if it is too high or too low
        if (point.y > maxHeight || point.y < minHeight)
            return false;

        if(input != Vector3.zero)
        {
            var maxAngle = 10f;
            var with = 0.03f;
            var left = Vector3.Cross(Vector3.up, input.normalized) * with;
            var leftAnglePos = footPos + left;
            var rightAnglePos = footPos - left;
            // Debugging
            var leftRotation = Quaternion.AngleAxis(maxAngle, Vector3.up);
            var rightRotation = Quaternion.AngleAxis(-maxAngle, Vector3.up);
            var leftDir = (leftRotation * input.normalized).normalized;
            var rightDir = (rightRotation * input.normalized).normalized;
            var footToPoint = point - footPos;
            Debug.DrawLine(leftAnglePos, rightAnglePos, Color.magenta);
            Debug.DrawRay(leftAnglePos, leftDir * Context.StepLength, Color.magenta);
            Debug.DrawRay(rightAnglePos, rightDir * Context.StepLength, Color.magenta);
            Debug.DrawLine(footPos, point, Color.red);

            // We dont care about the height, so we set the y to 0
            leftAnglePos.y = 0f;
            rightAnglePos.y = 0f;
            point.y = 0f;
            footToPoint.y = 0f;

            var leftPosToPoint = point - leftAnglePos;
            var rightPosToPoint = point - rightAnglePos;
            leftPosToPoint.y = 0f;
            rightPosToPoint.y = 0f;

            // First we check if it is left or right by using the left dir
            var isLeft = Vector3.Dot(left.normalized, footToPoint.normalized) > 0f;
            // Find the correct dir to use based on if it is left or right
            var dir = isLeft ? leftPosToPoint.normalized : rightPosToPoint.normalized;
            var dirToCompare = isLeft ? left.normalized : -left.normalized;
            // Now we check the angle between the left/right dir and the dir
            var angle = Vector3.Angle(dirToCompare, dir);
            // If the angle is too steep, we want to ignore it
            // But the normal forward angle is now 90 degrees
            if (angle < 90f - maxAngle)
                return false;
        }


        // If nothing is wrong, we can return true
        return true;
    }

    private bool ScanGroundObject(Vector3 input, out ScanInfo scanInfo)
    {
        var footPos = Context.Foot.Target.position;
        var otherFootPos = Context.OtherFoot.Target.position;
        var height = 0f;
        var defaultValues = Context.FootCastValues;
        scanInfo = new ScanInfo();
        
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
            
            // TODO
            // If it hits above, we should not set a safe point, 
            // We should set the correct height
            // And even shove it to the side if needed

            // DONE
            // We are checking if it is stuck
            // Then the movement is handled by the move logic
            
            var stuck = false;
            // If we hit it, we set the height to the hit point - foot size
            // -0.15f is to nullify the lifting default height
            if (Physics.BoxCast(position, size, Vector3.up, out var upHit, rotation,
                    dist + size.y,
                    Context.GroundLayers))
            {
                height = otherFootPos.y; // Should potentially be the hit point
                stuck = true;
            }
                
            scanInfo = new ScanInfo(height, stuck);
            return true;
        }
            
        // Check if we are actually on the ground
        if (Context.IsFootGrounded)
        {
            scanInfo = new ScanInfo(height, false);
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
            
        scanInfo = new ScanInfo(height, false);
        return true;
    }

    private enum EOffsetDirection
    {
        Left,
        Right,
        Backwards,
        Forwards
    }

    private Vector3 GetOffsetPosition(Vector3 relDir, float wantedHeight, float dist, EOffsetDirection offsetDir = EOffsetDirection.Right)
    {
        // Maybe implement a dot product to see if we should offset forwards or backwards also?
        
        var right = -Vector3.Cross(relDir.normalized, Vector3.up) * dist;
        
        // If the foot is on the left side, we want it to move to the left
        if(Context.Foot.Side == Foot.EFootSide.Left)
            right = -right;
        
        // If walking backwards, we want to move the foot to the other side
        var dot = Vector3.Dot(Context.Player.Camera.GetCameraYawTransform().forward.normalized, Context.Player.RelativeMoveInput.normalized);
        if (dot < -0.2f)
            right = -right;
        
        return new Vector3(Context.OtherFoot.Position.x, wantedHeight, Context.OtherFoot.Position.z) + right;
    }
    
    private Vector3 ClampedFootPosition(Vector3 footPos, Vector3 otherFootPos, Vector3 direction, float threshold = 0f)
    {
        // Offset the start position of the line to avoid not finding an intersection point when we should
        var lineStart = footPos - direction.normalized;
        var stepLenght = Context.StepLength - threshold;
        
        // We dont want the otherFootIntersection to do anything on the y axis
        // So we set the other foot pos to the same y as the foot
        var footHeight = footPos.y;
        var otherPosSim = new Vector3(otherFootPos.x, footHeight, otherFootPos.z);
        // We also cant let the simulation go above or below step StepLength
        var maxHeight = otherFootPos.y + stepLenght - 0.01f;
        var minHeight = otherFootPos.y - stepLenght + 0.01f;
        if (otherPosSim.y > maxHeight)
            otherPosSim.y = maxHeight;
        else if (otherPosSim.y < minHeight)
            otherPosSim.y = minHeight;
        
        // Dir from other foot to pos
        var otherDir = (direction + footPos) - otherPosSim;
            
        // We check the other intersection point now, in case the normal one fails, this one will never fail
        CalculateIntersectionPoint(otherFootPos, stepLenght, otherPosSim, otherDir.normalized,
            out var otherFootIntersect);
        
        // If we don't fail, we should use this intersection point for more accurate movement
        // Else we use the other foot intersection point
        if (!CalculateIntersectionPoint(otherFootPos, stepLenght, lineStart, direction.normalized,
                out var footIntersect))
            return otherFootIntersect;
        
        // Only start lerping when we are close enough to the edge && when input is pressed
        if(InputManager.Instance.MoveInput == Vector3.zero)
            return footIntersect;
        
        var startDist = stepLenght * 0.1f;
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

        RotateRigidbody(Context.Foot.Target, footForward, 350f);
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