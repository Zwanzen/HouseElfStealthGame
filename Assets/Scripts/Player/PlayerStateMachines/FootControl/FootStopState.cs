using UnityEngine;

/// <summary>
/// This state is used when the foot is no longer needs to be controlled by the foot control state machine.
/// When this state has triggered, the rigidbodies are set to kinematic, the IK weights are turned off, and all colliders are disabled.
/// When this state is done, it will transition to the idle state.
/// </summary>
public class FootStopState : FootControlState
{
    public FootStopState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }
    
    private bool _hasStopped;

    public override FootControlStateMachine.EFootState GetNextState()
    {
        // If we have stopped, we can go to idle
        if (_hasStopped)
        {
            return FootControlStateMachine.EFootState.Idle;
        }
        return StateKey;
    }
    
    public override void EnterState()
    {
        _hasStopped = false;
        Context.FootIKEffector.rotationWeight = 0f;

        // Disable the foot weights
        /*
        Context.FootIKEffector.positionWeight = 0f;

        // Set velocity to zero, not sure if this is needed
        Context.Foot.Target.linearVelocity = Vector3.zero;
        Context.Foot.Target.angularVelocity = Vector3.zero;
        Context.Foot.Thigh.linearVelocity = Vector3.zero;
        Context.Foot.Thigh.angularVelocity = Vector3.zero;
        Context.Foot.Calf.linearVelocity = Vector3.zero;
        Context.Foot.Calf.angularVelocity = Vector3.zero;

        // Set everything to kinematic
        Context.Foot.Target.isKinematic = true;
        Context.Foot.Thigh.isKinematic = true;
        Context.Foot.Calf.isKinematic = true;

        // Disable the colliders
        Context.Foot.Collider.enabled = false;
        Context.Foot.ThighCollider.enabled = false;
        Context.Foot.CalfCollider.enabled = false;
        */
        _hasStopped = true;
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