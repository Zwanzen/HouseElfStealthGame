using Unity.VisualScripting;
using UnityEngine;
using static CircleLineIntersection;
using static RigidbodyMovement;
using Pathfinding;
using Unity.Collections;
using UnityEngine.Windows;

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

    public override void EnterState()
    {
        // get the start angle
        _startAngle = Context.Foot.Target.transform.localRotation.x;

    }

    public override void ExitState()
    {
        _liftTimer = 0f;
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
        // Define main variables
        var footPos = Context.Foot.Position;
        var otherFootPos = Context.OtherFoot.Position;
        var input = Context.Player.RelativeMoveInput;

        // Define base heights
        var baseFootLiftedHeight = 0.1f;
        var wantedHeight = otherFootPos.y + baseFootLiftedHeight;

        // Now we get the height prediction info
        // This is important, because the foot direction might need to change.
        // If we want to move up, but are stuck, the prediction gives us a direction to move in.
        if (TryHeightPrediction(input, footPos, otherFootPos, out var prediction))
        {
            // If we are stuck, we want to set the move position to the prediction direction + the foot position.
            if (prediction.Stuck)
            {
                // We want to move in the direction of the prediction
                var fixPos = footPos + prediction.FixDir;
                // If the distance is too far, clamp it
                if (Vector3.Distance(fixPos, otherFootPos) > Context.StepLength)
                    fixPos = ClampedFootPosition(footPos, otherFootPos, (fixPos - footPos).normalized);
                MoveToRigidbody(Context.Foot.Target, fixPos, Context.CurrentWalkSettings);
                return;
            }

            // Decides that height the foot should be at
            if (prediction.Height < otherFootPos.y - 0.15f)
                wantedHeight = otherFootPos.y - 0.15f;
            wantedHeight = prediction.Height + baseFootLiftedHeight;
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

    /// <summary>
    /// Struct used to store the prediction info for the foot.
    /// </summary>
    private struct PredictionInfo
    {
        public readonly float Height;
        public readonly bool Stuck;
        public readonly Vector3 FixDir;

        public PredictionInfo(float height, bool stuck, Vector3 fixDir)
        {
            Height = height;
            Stuck = stuck;
            FixDir = fixDir;
        }
    }

    private RaycastHit[] _hits = new RaycastHit[20];

    /// <summary>
    /// Calculates the target height position we want to go to depending on the input.
    /// </summary>
    private bool TryHeightPrediction(Vector3 input, Vector3 footPos, Vector3 otherPos, out PredictionInfo predictionInfo)
    {
        // Sets it to default values
        predictionInfo = new PredictionInfo(0f, false, Vector3.zero);

        // Dynamic Check Position
        var forwardFootPos = Context.Foot.XZForward * 0.1f + footPos;
        var checkPos = forwardFootPos - input * 0.1f;

        // We first scan for points and store the hits if there are any.
        if (!ScanForPoints(checkPos, otherPos, input, _hits, out var hitCount))
            return false; // If there are no points, we dont have any prediction info.

        // If there are points, we want to find *The* valid point if there are any.
        if(!FindValidPoint(hitCount, checkPos, footPos, otherPos, input, out var validPos))
            return false; // If there are no valid points, we dont have any prediction info.

        // If we have a valid point, we have to check if we are stuck getting there.
        var stuck = IsStuckPoint(validPos, footPos, otherPos, input, out var stuckFixDir);

        // Now assign the values to the prediction info for the movement to handle.
        predictionInfo = new PredictionInfo(validPos.y, stuck, stuckFixDir);
        return true; // We have a valid point, so we return true.
    }

    /// <summary>
    /// Scans for points around the foot, and returns the amount of hits found.
    /// And it also stores the hits in the a defined array using non alloc.
    /// </summary>
    /// <returns>If there are any points, and how many.</returns>
    private bool ScanForPoints(Vector3 checkPos, Vector3 otherPos, Vector3 input, RaycastHit[] result, out int hitCount)
    {
        var radius = Context.StepLength * 0.5f;
        var castHeight = otherPos.y + Context.StepHeight + radius;
        var castPos = new Vector3(checkPos.x, castHeight, checkPos.z);
        // We want to offset the cast position in the direction of the input
        castPos += input.normalized * 0.2f;
        var dist = Context.StepHeight * 2f + radius;

        hitCount = Physics.SphereCastNonAlloc(castPos, radius, Vector3.down, result, dist, Context.GroundLayers);
        return hitCount > 0;
    }

    /// <summary>
    /// Used to sift through the points we found around the foot,
    /// and find a valid point to use.
    /// </summary>
    /// <returns>The position of the valid hit.</returns>
    private bool FindValidPoint(int hitCount, Vector3 checkPos, Vector3 footPos, Vector3 otherPos, Vector3 input, out Vector3 validPos)
    {
        // We need to efficiently store the reachable points
        // Create with maximum possible size (hitCount)
        validPos = Vector3.zero;
        var hitPoints = new NativeArray<RaycastHit>(hitCount, Allocator.Temp);
        int validPointCount = 0;
        for (var i = 0; i < hitCount; i++)
        {
            if (IsReachablePoint(_hits[i].point, checkPos, footPos, otherPos, input))
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

        // If there is no input, check if there is a close point,
        // If not, we want to return false
        var closestPoint = Vector3.zero;
        var forwardFootPos = footPos + Context.Foot.Target.transform.forward * 0.1f;
        var footXZPos = new Vector3(forwardFootPos.x, 0f, forwardFootPos.z);
        var closestXZPoint = Vector3.zero;
        if (input == Vector3.zero)
        {
            // Go throught all the points and check if they are close enough
            var storedDist = 0f;
            for (var i = 0; i < validPointCount; i++)
            {
                var d = Vector3.Distance(hitPoints[i].point, forwardFootPos);
                var xzp = hitPoints[i].point;
                xzp.y = 0f;
                var xzDist = Vector3.Distance(xzp, footXZPos);
                // If xz distance is too far, we skip it
                if (xzDist > 0.1f)
                    continue;
                else if (closestPoint == Vector3.zero)
                {
                    // If it has no point, we want to use the first point
                    closestPoint = hitPoints[i].point;
                    storedDist = d;
                    continue;
                }
                else
                {
                    // Check if the new point is closer
                    if (d < storedDist)
                    {
                        storedDist = d;
                        closestPoint = hitPoints[i].point;
                    }
                }
            }

            // If we have a point, we want to use it
            if (closestPoint != Vector3.zero)
            {
                validPos = closestPoint;
                // Always dispose NativeArray when done
                hitPoints.Dispose();
                return true;
            }

            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return false;
        }

        // If there is only one point, we want to use that point
        // But only if we have pressed the input
        if (validPointCount == 1)
        {
            validPos = hitPoints[0].point;
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return true;
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
            closestPoint = GetClosestXZPoint(hitPoints, validPointCount, footPos, out closestXZPoint, true);
            // We want to use the point closest to the foot on the xz plane
            validPos = closestPoint;
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return true;
        }

        // If nothing is above, we want to use the point closest to the foot
        closestPoint = GetClosestXZPoint(hitPoints, validPointCount, footPos, out closestXZPoint);
        // Only if it is close enough to the foot, we want to use it
        if (Vector3.Distance(closestXZPoint, footXZPos) < 0.2f)
        {
            validPos = closestPoint;
            // Always dispose NativeArray when done
            hitPoints.Dispose();
            return true;
        }

        // Always dispose NativeArray when done
        hitPoints.Dispose();
        return false;
    }

    /// <summary>
    /// Cluncky helper function for valid point finding.
    /// </summary>
    private Vector3 GetClosestXZPoint(NativeArray<RaycastHit> hitPoints, int count, Vector3 footPos, out Vector3 xzPoint, bool useMinDist = false)
    {
        var footXZPos = new Vector3(footPos.x, 0f, footPos.z);
        var closestPoint = Vector3.zero;
        xzPoint = Vector3.zero;
        var storedDist = 0f;
        for (var i = 0; i < count; i++)
        {
            var xzHit = hitPoints[i].point;
            xzHit.y = 0f;
            var d = Vector3.Distance(xzHit, footXZPos);
            if (hitPoints[i].point.y < footPos.y && useMinDist)
                if (d < 0.25f)
                    continue;

            // If it has no point, we want to use the first point
            if (closestPoint == Vector3.zero)
            {
                closestPoint = hitPoints[i].point;
                storedDist = d;
                xzPoint = xzHit;
                continue;
            }
            // Check if the new point, has a similar distance as the stored point
            // If they are similar, we want to use the one that is closest to the foot
            var diff = Mathf.Abs(storedDist - d);
            var threshold = 0.02f;

            // If the distances are similar within the threshold
            if (diff < threshold)
            {
                // If the new point is higher than the current point, use the new point
                if (hitPoints[i].point.y > closestPoint.y)
                {
                    storedDist = d;
                    xzPoint = xzHit;
                    closestPoint = hitPoints[i].point;
                }
            }

            // Check if the point is closer than the closest point
            if (d < storedDist)
            {
                storedDist = d;
                xzPoint = xzHit;
                closestPoint = hitPoints[i].point;
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// Checks if the point is reachable based on various conditions.
    /// Returns true if non of the fail conditions are met.
    /// </summary>
    private bool IsReachablePoint(Vector3 point, Vector3 checkPos, Vector3 footPos, Vector3 otherPos, Vector3 input)
    {
        // Check if it is too far way
        if (Vector3.Distance(otherPos, point) > Context.StepLength)
            return false;

        var maxHeight = Context.StepHeight + otherPos.y - 0.035;
        var minHeight = otherPos.y - Context.StepHeight;

        // Check if it is too high or too low
        if (point.y > maxHeight || point.y < minHeight)
            return false;

        if (input != Vector3.zero)
        {
            // If the point is behind the scan,
            // its not reachable
            var footToPoint = point - footPos;
            if (Vector3.Dot(footToPoint.normalized, input.normalized) < 0f)
                return false;

            var maxAngle = 10f;
            var with = 0.03f;
            var left = Vector3.Cross(Vector3.up, input.normalized) * with;
            var leftAnglePos = checkPos + left;
            var rightAnglePos = checkPos - left;
            // Debugging
            var leftRotation = Quaternion.AngleAxis(maxAngle, Vector3.up);
            var rightRotation = Quaternion.AngleAxis(-maxAngle, Vector3.up);
            var leftDir = (leftRotation * input.normalized).normalized;
            var rightDir = (rightRotation * input.normalized).normalized;
            Debug.DrawLine(leftAnglePos, rightAnglePos, Color.magenta);
            Debug.DrawRay(leftAnglePos, leftDir * Context.StepLength, Color.magenta);
            Debug.DrawRay(rightAnglePos, rightDir * Context.StepLength, Color.magenta);
            Debug.DrawLine(checkPos, point, Color.red);

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

        // Adjust the xz distance check based on the player current speed
        var xzPos = new Vector3(point.x, 0f, point.z);
        var forwaredFootPos = footPos + Context.Foot.Target.transform.forward * 0.1f;
        var footXZPos = new Vector3(forwaredFootPos.x, 0f, forwaredFootPos.z);
        var dist = Vector3.Distance(xzPos, footXZPos);
        var maxDist = Mathf.Lerp(0.1f, Context.StepLength * 0.5f, Context.Player.CurrentPlayerSpeed);
        // If the distance is too far, we want to ignore it
        if (dist > maxDist)
            return false;

        // If nothing is wrong, we can return true
        return true;
    }

    /// <summary>
    /// Scans if moving up to the point is possible, and if not, - 
    /// what direction we need to move to be able to reach the height of the point.
    /// </summary>
    private bool IsStuckPoint(Vector3 point, Vector3 footPos, Vector3 otherPos, Vector3 input, out Vector3 stuckFixDir)
    {
        stuckFixDir = Vector3.zero;
        // Get the box cast values fitting our fot collider
        var defaultValues = Context.FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = Context.Foot.Rotation;
        size *= 1.01f;
        position += Context.Foot.Transform.up * 0.01f;
        if (!Physics.CheckBox(position, size, rotation, Context.GroundLayers))
            return false; // If we are not stuck, we return.

        // If we are stuck, we want to find what direction to move
        // in order to not get stuck when moving up to point height.
        stuckFixDir = -Context.Player.Camera.GetCameraYawTransform().forward;
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