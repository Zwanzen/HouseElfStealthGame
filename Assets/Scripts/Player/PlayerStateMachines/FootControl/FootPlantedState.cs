using UnityEngine;
using static CircleLineIntersection;

public class FootPlantedState : FootControlState
{
    public FootPlantedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if(!Context.IsFootGrounded)
            return FootControlStateMachine.EFootState.Falling;
        
        if (Context.IsFootLifting)
            return FootControlStateMachine.EFootState.Lifted;
        
        return StateKey;
    }
    
    private static RigidbodyConstraints PlacedConstraints => GetPlacedConstraints();
    private static RigidbodyConstraints LiftedConstraints => RigidbodyConstraints.FreezeRotation;
    
    public override void EnterState()
    {
        //Context.Foot.Target.isKinematic = true;
        Context.FootSoundPlayer.PlayFootSound(PlayerFootSoundPlayer.EFootSoundType.Wood);
        var rb = Context.Foot.Target;
        rb.constraints = PlacedConstraints;
    }

    public override void ExitState()
    {
        //Context.Foot.Target.isKinematic = false;
        var rb = Context.Foot.Target;
        rb.constraints = LiftedConstraints;
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        MoveToGround();
    }
    
    private static RigidbodyConstraints GetPlacedConstraints()
    {
        return RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;
    }

    private void MoveToGround()
    {
        var dir = Vector3.down;

        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dir.magnitude;
        var breakDistance = 0.05f;
        var magLerp = mag / breakDistance;
        dir.Normalize();
        dir *= Context.SpeedCurve.Evaluate(magLerp);
        Context.MoveFootToPosition(dir);
    }
}