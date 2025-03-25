using UnityEngine;
using static RigidbodyMovement;

public class PlacingSneakState : ProceduralSneakState
{
    public PlacingSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }

    private bool _validPlacement;
    private bool _placed;
    private RaycastHit _cast;
    private Vector3 _placeNormal;
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (!Context.Player.IsGrounded)
        {
            return ProceduralSneakStateMachine.ESneakState.Stop;
        }
        
        if (_placed)
        {
            return ProceduralSneakStateMachine.ESneakState.Planted;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        _validPlacement = false;
        _placed = false;
        _startPos = Context.LiftedFoot.position;
        Context.ResetLiftedFootGoalVel();
        Context.ResetBodyGoalVel();
        
        // Check if the foot placement is valid
        if (Physics.SphereCast(Context.LiftedFoot.position, Context.FootPlaceOffset, Vector3.down, out var hit,
                Context.SneakStepHeight * 0.9f, Context.GroundLayers))
        {
            _validPlacement = true;
            _cast = hit;
            _placeNormal = _cast.normal;
        }
    }

    public override void ExitState()
    {
        if(_validPlacement)
            Context.PlaySound(Context.LiftedFoot,Context.GetGroundTypeFromFoot(Context.LiftedFoot));
    }

    public override void UpdateState()
    {
        var footPos = new Vector3(Context.LiftedFoot.position.x, Context.LiftedFoot.position.y - Context.FootPlaceOffset, Context.LiftedFoot.position.z);
        var distToGround = Vector3.Distance(footPos, _checkCast.point);
        if (distToGround < 0.05f)
        {
            _placed = true;
        }
    }

    private Vector3 _startPos;
    private RaycastHit _checkCast;
    
    public override void FixedUpdateState()
    {
        Physics.Raycast(Context.LiftedFoot.position + Vector3.up, Vector3.down, out _checkCast, 10f, Context.GroundLayers);
        
        if (_validPlacement)
        {
            MoveToValidPosition();
        }
        else
        {
            Context.MoveLiftedFoot(Vector3.down);
            var distFromStart = Vector3.Distance(Context.LiftedFoot.position, _startPos);
            if (distFromStart > Context.SneakStepHeight * 0.8f)
            {
                Context.Player.SetPlayerStumble(true);
            }
        }
        
        Context.MoveBody(Context.GetFeetMiddlePoint());
        Context.UpdateFootGroundNormal(Context.PlantedFoot);
        HandleFootRotation();
    }

    private void MoveToValidPosition()
    {
        // Ground Pos
        var groundPos = _checkCast.point; //Context.GetFootGroundPosition(Context.LiftedFoot);
        var direction = groundPos - Context.LiftedFoot.position;
        
        var maxDist = Vector3.Distance(groundPos, _startPos);
        var distToPos = Vector3.Distance(Context.LiftedFoot.position, groundPos);
        var lerp = distToPos / maxDist;

        var maxDirection = direction.normalized * Context.SneakStepLength;
        var lerpedDirection = Vector3.Lerp(direction, maxDirection, Context.SpeedCurve.Evaluate(lerp));
        
        Context.MoveLiftedFoot(lerpedDirection);
    }
    
    private void HandleFootRotation()
    {
        var foot = Context.LiftedFoot;
        var footForward = foot.transform.forward;
        var direction = Vector3.ProjectOnPlane(footForward, _placeNormal);
        
        // Lerp speed based on distance to ground
        var dist = Vector3.Distance(foot.position, _cast.point);
        var maxDist = _cast.distance;
        
        var lerpSpeed = Mathf.Lerp(500f, 0f, Context.PlaceSpeedCurve.Evaluate(dist / maxDist));
        
        RotateRigidbody(foot,direction, lerpSpeed);
    }
}