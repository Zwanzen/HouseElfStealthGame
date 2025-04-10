﻿using UnityEngine;

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
    private bool _placed;
    private RaycastHit _cast;
    private Vector3 _placeNormal;
    
    private Vector3 _startPos;
    private RaycastHit _checkCast;
    
    public override void EnterState()
    {
        _validPlacement = false;
        _placed = false;
        _startPos = Context.Foot.Target.position;
        
        // Check if the foot placement is valid
        if (Physics.SphereCast(Context.Foot.Target.position, Context.FootPlaceOffset.magnitude, Vector3.down, out var hit,
                Context.StepHeight * 0.9f, Context.GroundLayers))
        {
            _validPlacement = true;
            _cast = hit;
            _placeNormal = _cast.normal;
        }
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
        //HandleFootRotation();
    }

    private bool CheckStuckOnLedge(out RaycastHit hit)
    {
        // Do a box cast to check if the foot is stuck on a ledge
        var position = Context.FootCastValues.Position;
        var size = Context.FootCastValues.Size;
        var rotation = Context.FootCastValues.Rotation;
        
        position.y += size.y;
        size *= 1.04f;

        return Physics.BoxCast(position, size, Vector3.down, out hit, rotation, size.y * 1.5f,
            Context.GroundLayers);
    }
    
    private void MoveToGround()
    {
        var dir = Vector3.down;
        // Check if the foot is stuck on a ledge
        if (CheckStuckOnLedge(out var stuckHit))
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