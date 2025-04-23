using UnityEngine;

/// <summary>
/// This state is used when the foot is no longer controlled by the foot control state machine.
/// The only way to exit this state is to call TransitionToState() from the foot SM.
/// </summary>
public class FootIdleState : FootControlState
{
    public FootIdleState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key){Context = context;}
    public override FootControlStateMachine.EFootState GetNextState() {return StateKey;}

    private Vector3 _position;
    private Quaternion _rotation;
    public override void EnterState()
    {
        Context.BodyIK.solver.OnPreSolve += OnPreSolve;
    }
    public override void ExitState(){}

    public override void UpdateState()
    {

    }

    public override void FixedUpdateState()
    {
        RigidbodyMovement.MoveToRigidbody(Context.Foot.Target, _position, Context.PlacementSettings);
        // Rotate the rotation 90 degrees around the up axis
        Context.Foot.Target.transform.rotation = Context.Foot.FootBoneRotation;
    }

    private void OnPreSolve()
    {
        _position = Context.FootIKEffector.GetNode(Context.BodyIK.solver).offset + Context.FootIKEffector.bone.position;
        _rotation = Quaternion.LookRotation(Context.FootIKEffector.planeBone2.position - Context.FootIKEffector.planeBone1.position, Context.FootIKEffector.planeBone3.position - Context.FootIKEffector.planeBone1.position);
    }
}