using UnityEngine;

public class FootStartState : FootControlState
{
    public FootStartState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }
    
    private bool _hasStarted;

    public override FootControlStateMachine.EFootState GetNextState()
    {
        return _hasStarted ? FootControlStateMachine.EFootState.Falling : StateKey;
    }
    
    public override void EnterState()
    {
        _hasStarted = false; // Make sure we run all logic in this state
        
        // If we are falling, and want to stand up,
        // we need to make sure the target is not moving
        Context.Foot.Target.linearVelocity = Vector3.zero;
        
        // We set the foot to the rest position
        // This should be the position of the foot when it is standing
        Context.Foot.Target.position = Context.Foot.RestTarget.position;
        
        // We set the foots rotation to the body forward direction
        var forward = Quaternion.LookRotation(Context.Player.Rigidbody.transform.forward, Vector3.up);
        Context.Foot.Target.rotation = forward;
        
        // Now we place the foot on the ground
        if (Physics.SphereCast(Context.Foot.Target.position + Vector3.up, Context.FootRadius, Vector3.down, out var hit, Context.StepHeight + 1f, Context.GroundLayers))
        {
            Context.Foot.Target.position = hit.point + Context.FootPlaceOffset;
        }
        
        // Enable the foot weights
        Context.FootIKEffector.positionWeight = 1f;
        Context.FootIKEffector.rotationWeight = 1f;
        
        _hasStarted = true; // Now we have enabled the foot
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
    }
}