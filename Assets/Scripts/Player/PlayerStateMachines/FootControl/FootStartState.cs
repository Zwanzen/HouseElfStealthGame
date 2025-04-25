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
        Context.Foot.Target.position = Context.Foot.FootBonePosition;
        Context.Foot.Target.rotation = Context.Foot.FootBoneRotation;
        
        // Set the foot target position to the closest point on the ground
        if(Context.FootGroundCast(0.2f,out var hit))
            Context.Foot.Target.position = hit.point + Vector3.up * Context.FootRadius;

        Context.Foot.Thigh.position = Context.Foot.ThighPosition;
        Context.Foot.Thigh.rotation = Context.Foot.ThighRotation;
        Context.Foot.Calf.position = Context.Foot.CalfPosition;
        Context.Foot.Calf.rotation = Context.Foot.CalfRotation;
        
        // Set everything to non-kinematic
        Context.Foot.Target.isKinematic = false;
        Context.Foot.Thigh.isKinematic = false;
        Context.Foot.Calf.isKinematic = false;
        
        // Make sure the feet have no velocity
        Context.Foot.Target.linearVelocity = Vector3.zero;
        Context.Foot.Target.angularVelocity = Vector3.zero;
        Context.Foot.Thigh.linearVelocity = Vector3.zero;
        Context.Foot.Thigh.angularVelocity = Vector3.zero;
        Context.Foot.Calf.linearVelocity = Vector3.zero;
        Context.Foot.Calf.angularVelocity = Vector3.zero;
        
        // Enable the colliders
        Context.Foot.Collider.enabled = true;
        Context.Foot.ThighCollider.enabled = true;
        Context.Foot.CalfCollider.enabled = true;
        
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