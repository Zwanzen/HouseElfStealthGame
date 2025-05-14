using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FootPlantedState : FootControlState
{
    public FootPlantedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if (Context.IsFootLifting)
            return FootControlStateMachine.EFootState.Lifted;
        
        return StateKey;
    }
    
    private Rigidbody _rb;
    private static RigidbodyConstraints PlacedConstraints => GetPlacedConstraints();
    private static RigidbodyConstraints LiftedConstraints => RigidbodyConstraints.FreezeRotation;
    
    
    public override void EnterState()
    {
        //Context.Foot.Target.isKinematic = true;
        _rb = Context.Foot.Target;
        _rb.constraints = PlacedConstraints;
        _rb.useGravity = true;
    }

    
    public override void ExitState()
    {
        //Context.Foot.Target.isKinematic = false;
        _rb = Context.Foot.Target;
        _rb.constraints = LiftedConstraints;
        _rb.useGravity = false;
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        HandleRotation();
        //MoveToGround();
    }
    
    private static RigidbodyConstraints GetPlacedConstraints()
    {
        return RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;
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
}