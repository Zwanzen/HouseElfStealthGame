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
        
        if(_shouldFall)
            return FootControlStateMachine.EFootState.Falling;
        
        return StateKey;
    }

    private bool _shouldFall;
    
    private bool _validPlacement;
    private bool _placed;
    private RaycastHit _cast;
    private Vector3 _placeNormal;
    
    private Vector3 _startPos;
    private RaycastHit _checkCast;
    
    public override void EnterState()
    {
        _shouldFall = false;
        CheckForFall();
        _validPlacement = false;
        _placed = false;
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
        CheckForFall();
        MoveToGround();
        //HandleFootRotation();
    }

    private void CheckForFall()
    {
        // Check if the foot has ground
        var hasGround = Context.FootGroundCast();
        // Check if the foot is below the other foot
        var foot = Context.Foot.Target.position;
        var otherFoot = Context.OtherFoot.Target.position;
        var dist = Vector3.Distance(foot, otherFoot);
        var isBelow = foot.y < otherFoot.y;
        
        // Check if the foot is below the other foot and has no ground
        if (!hasGround)
        {
            _shouldFall = true;
        }
    }
    
    private bool CheckForGround()
    {
        var defaultValues = Context.FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        position.y += size.y;

        var foot = Context.Foot.Target.position;
        var otherFoot = Context.OtherFoot.Target.position;
        var lineStart = foot + Vector3.up;
        
        var dist = 0f;
        CircleLineIntersection.CalculateIntersectionPoint(otherFoot, Context.StepLength, lineStart, Vector3.down, out var intersectionPoint);
        dist = Vector3.Distance(foot, intersectionPoint);
        
        // Check if the foot placement is valid
        if (Physics.BoxCast(position, size, Vector3.down, out _cast, rotation, dist, Context.GroundLayers))
        {
            _validPlacement = true;
            _placeNormal = _cast.normal;
            return true;
        }

        return false;
    }
    
    private void MoveToGround()
    {
        var dir = Vector3.down;
        // Check if the foot is stuck on a ledge
        if (Context.CheckStuckOnLedge(out var stuckHit) && !Context.IsFootGrounded)
        {
            // If the foot is stuck on a ledge, move it away
            var other = stuckHit.collider.ClosestPoint(Context.Foot.Target.position);
            dir = (Context.Foot.Target.position + Vector3.down - other).normalized;
            Context.MoveFootToPosition(dir);
            return;
        }

        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dir.magnitude;
        var breakDistance = 0.1f;
        var magLerp = mag / breakDistance;
        dir.Normalize();
        dir *= Context.SpeedCurve.Evaluate(magLerp);
        
        Context.MoveFootToPosition(dir);
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