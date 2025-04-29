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
        _startPos = Context.Foot.Target.position;
        Context.Foot.Target.useGravity = true;
    }

    public override void ExitState()
    {
        Context.Foot.Target.useGravity = false;
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        //MoveToGround();
        HandleRotation();
    }
    
    private void MoveToGround()
    {
        var dir = Vector3.down;

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
        
        RigidbodyMovement.MoveRigidbody(Context.Foot.Target, dir, Context.CurrentSneakSettings);
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