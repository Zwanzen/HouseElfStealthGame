using UnityEngine;

/// <summary>
/// This state is used to initialize the control of the foot.
/// When this state is entered, every rigidbody is set to non-kinematic and the colliders are enabled.
/// When this state is done, it will transition to the falling state.
/// </summary>
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
        _hasStarted = false;
        
        // We set the positions and rotations to the bones
        Context.Foot.Target.position = Context.Foot.RestPosition;
        Context.Foot.Target.rotation = Quaternion.LookRotation(Context.Foot.RestDirection, Vector3.up);

        // Set the foot target position to the closest point on the ground
        if (Context.FootGroundCast(0.2f,out var hit))
            Context.Foot.Target.position = hit.point + Vector3.up * Context.FootRadius;

        // Set everything to non-kinematic
        Context.Foot.Target.isKinematic = false;
        
        // Make sure the feet have no velocity
        Context.Foot.Target.linearVelocity = Vector3.zero;
        Context.Foot.Target.angularVelocity = Vector3.zero;
        
        // Enable the colliders
        Context.Foot.Collider.enabled = true;
        Context.Foot.Thigh.enabled = true;
        Context.Foot.Calf.enabled = true;
        
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