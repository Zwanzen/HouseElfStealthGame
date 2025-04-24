using UnityEngine;

public class FootPlacingState : FootControlState
{
    public FootPlacingState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if(Context.IsFootGrounded)
            return FootControlStateMachine.EFootState.Planted;

        
        return StateKey;
    }

    private bool _validPlacement;
    private RaycastHit _cast;
    private Vector3 _placeNormal;
    
    private Vector3 _startPos;
    private RaycastHit _checkCast;
    
    public override void EnterState()
    {
        _validPlacement = CheckValidPlacement();
        if(!_validPlacement)
            Context.Player.Camera.Stumble();
        _startPos = Context.Foot.Target.position;
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        MoveToGround();
        HandleRotation();
    }
    
    private bool CheckValidPlacement()
    {
        return Context.FootGroundCast(0.5f);
    }
    
    private void MoveToGround()
    {
        var dir = Vector3.down;
        var settingsToUse = Context.SneakMovementSettings;
        if (!_validPlacement)
        {
            dir = Vector3.down + Context.Foot.Target.position; // dirPos
            settingsToUse = Context.PlacementSettings;
            
            // Determine what the safe position is
            var lastSafe = Context.LastSafePosition;
            var oldSafe = Context.OldSafePosition;
            var distToLast = Vector3.Distance(lastSafe, Context.OtherFoot.Target.position);
            var distToOld = Vector3.Distance(oldSafe, Context.OtherFoot.Target.position);
            var safe = distToLast < distToOld ? lastSafe : oldSafe;
            
            var safePos = safe + (0.3f * Vector3.up);
            
            var xzSafePos = safePos;
            xzSafePos.y = 0f;
            var xzFootPos = Context.Foot.Target.position;
            xzFootPos.y = 0f;
            var safeMag = (xzSafePos - xzFootPos).magnitude;
            
            var downDistance = Context.FootRadius;
            var lerp = safeMag / downDistance;
            dir = Vector3.Lerp(dir, safePos, lerp);
            
            RigidbodyMovement.MoveToRigidbody(Context.Foot.Target, dir, settingsToUse);
            return;
        }

        /*
        // Check if the foot is stuck on a ledge
        if (Context.CheckStuckOnLedge(out var stuckHit) && !Context.IsFootGrounded && _validPlacement)
        {
            // If the foot is stuck on a ledge, move it away
            var other = stuckHit.collider.ClosestPoint(Context.Foot.Target.position);
            dir = (Context.Foot.Target.position + Vector3.down - other).normalized;
            Context.MoveFootToPosition(dir);
            return;
        }
        */
        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dir.magnitude;
        var breakDistance = 0.1f;
        var magLerp = mag / breakDistance;
        dir.Normalize();
        dir *= Context.SpeedCurve.Evaluate(magLerp);
        
        RigidbodyMovement.MoveRigidbody(Context.Foot.Target, dir, settingsToUse);
    }
    private void HandleRotation()
    {
        // Get the ground normal if we have one
        var normal = Vector3.up;
        if(Context.FootGroundCast(out var hit))
            normal = hit.normal;
        var direction = Vector3.ProjectOnPlane(Context.Foot.Target.transform.forward, normal);

        RigidbodyMovement.RotateRigidbody(Context.Foot.Target, direction, 500f);
    }
    private void HandleFootRotation()
    {
        var foot = Context.Foot.Target;
        var footForward = foot.transform.forward;
        var direction = Vector3.ProjectOnPlane(footForward, _placeNormal);
        
        // Lerp speed based on distance to ground
        var dist = Vector3.Distance(foot.position, _cast.point);
        var maxDist = _cast.distance;
        
        var lerpSpeed = Mathf.Lerp(500f, 0f, Context.PlaceCurve.Evaluate(dist / maxDist));
        
        RigidbodyMovement.RotateRigidbody(foot,direction, lerpSpeed);
    }
}