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
        _liftTimer += Time.deltaTime;
    }

    public override void FixedUpdateState()
    {
        var footPos = Context.Foot.Target.position;
        var otherFootPos = Context.OtherFoot.Target.position;

        // We want the foot to move upwards as much as possible first
        var wantedHeight = otherFootPos.y + 0.15f;
        
        // If lift is pressed, add height
        if (InputManager.Instance.IsLifting)
            wantedHeight += 0.2f; 
        
        // This is the current position of the foot, but at the wanted height
        var wantedHeightPos = new Vector3(footPos.x, wantedHeight, footPos.z);
        
        // We also calculate the input position based on the player input
        // But also take into account the wanted height
        var wantedInputPos = wantedHeightPos + Context.Player.RelativeMoveInput.normalized;
        
        // *** TEMP ***
        // If the foot is behind the other foot,
        // We want to move this foot towards an offset to the side of the other foot
        var distBehind = Context.RelativeDistanceInDirection(footPos, otherFootPos, Context.Player.RelativeMoveInput.normalized);
        var right = -Vector3.Cross(Context.Player.RelativeMoveInput.normalized, Vector3.up) * (Context.FootRadius * 4f);
        
        // If the foot is on the left side, we want it to move to the left
        if(Context.Foot.Side == Foot.EFootSide.Left)
            right = -right;
        
        // If walking backwards, we want to move the foot to the other side
        var dot = Vector3.Dot(Context.Player.Camera.GetCameraYawTransform().forward.normalized, Context.Player.RelativeMoveInput.normalized);
        if (dot < -0.2f)
            right = -right;
        
        // The position to the side of the other foot
        var offsetPos = new Vector3(otherFootPos.x, wantedHeight, otherFootPos.z) + right;
        offsetPos = wantedInputPos;

        
        // We lerp our input position with the offset position based on how far behind the foot is
        var wantedPos = Vector3.Lerp(wantedInputPos, offsetPos, distBehind/(Context.StepLength * 0.2f));
        
        // Depending on our distance to the wanted height compared to our current height
        // We lerp what position we want to go to
        var currentHeight = footPos.y - otherFootPos.y;
        var maxHeight = wantedHeight - otherFootPos.y;
        // Doesn't start to lerp until we are 50% of the way to the max height
        var posLerp = currentHeight / maxHeight;
        var pos = Vector3.Lerp(wantedHeightPos, wantedPos, Context.HeightCurve.Evaluate(posLerp));
        
        // We get the direction towards the wanted position
        var dirToPos = pos - footPos;
        Debug.DrawLine(otherFootPos, pos, Color.green);


        // Now clamp if we are going out of step length
        if (Vector3.Distance(pos, otherFootPos) > Context.StepLength)
            pos = ClampedFootPosition(footPos, otherFootPos, dirToPos);
        
        Debug.DrawLine(footPos, pos, Color.red);

        // Update dir to pos because we might have changed the position
        dirToPos = pos - footPos;
        
        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dirToPos.magnitude;
        var breakDistance = 0.05f;
        var magLerp = mag / breakDistance;
        dirToPos.Normalize();
        dirToPos *= Context.SpeedCurve.Evaluate(magLerp);
        
        Context.MoveFootToPosition(dirToPos);
        HandleFootRotation();
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
        if (!CalculateIntersectionPoint(otherFootPos, Context.StepLength, lineStart, direction.normalized,
                out var footIntersect))
            return otherFootIntersect;
        
        // Only start lerping when we are close enough to the edge
        var startDist = Context.StepLength * 0.1f;
        var distToIntersect = Vector3.Distance(footPos, footIntersect);
        var lerp = distToIntersect / startDist;
        
        // The closer we are to the edge, the more we want to use the other foot intersection point
        return Vector3.Lerp(otherFootIntersect, footIntersect, lerp);
    }
    
    
    private Vector3 _forward;
    private Vector3 _right;
    private float _liftTimer;
    private float _startAngle;
    private void HandleFootRotation()
    {
        var isMoving = InputManager.Instance.MoveInput.magnitude > 0.01f;
        
        // Store the camera forward direction if we are moving
        if (isMoving)
        {
            // Other direction
            var otherDir = Context.Player.RelativeMoveInput.normalized;
            var dot = Vector3.Dot(Context.Player.Camera.GetCameraYawTransform().forward.normalized, otherDir.normalized);
            if (dot < -0.2f)
                otherDir = -otherDir;
            
            // Downwards lerp
            var camAngle = Context.Player.Camera.CameraX;
            _forward = Vector3.Lerp(otherDir, Context.Player.Camera.GetCameraYawTransform().forward.normalized, camAngle/60f);
            
            _right = Vector3.Lerp(-Vector3.Cross(otherDir, Vector3.up), Context.Player.Camera.GetCameraYawTransform().right.normalized, camAngle/60f);
        }
        
        // Update the lifted foot pitch
        var minPitch = 85f;
        var maxPitch = -85f;
        
        var minRelDist = -Context.StepLength;
        var maxRelDist = Context.StepLength;
        var relDist = Context.RelativeDistanceInDirection(Context.OtherFoot.Target.position, Context.Foot.Target.position, _forward);
        
        // Lerp Foot Pitch
        var angle = Mathf.Lerp(minPitch, maxPitch, Mathf.InverseLerp(minRelDist, maxRelDist, relDist));
        var lerpAngle = Mathf.Lerp(_startAngle, angle, Context.PlaceCurve.Evaluate(_liftTimer / 0.20f));
        
        // *** TMEP ***
        lerpAngle = _startAngle;
        
        // Rotate the camForward direction around the foot's right direction
        var footForward = Quaternion.AngleAxis(lerpAngle, _right) * _forward;
        footForward.Normalize();

        if(footForward == Vector3.zero)
            return;
        
        RigidbodyMovement.RotateRigidbody(Context.Foot.Target, footForward, 500f);
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