using UnityEngine;

public class PlayerGroundedState : PlayerControlState
{
    public PlayerGroundedState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        var dist = Vector3.Distance(Context.LeftFootTarget.position, Context.RightFootTarget.position);
        
        if (!Context.IsGrounded() || dist > 0.8f)
        {
            return PlayerControlStateMachine.EPlayerControlState.Falling;
        }


        return StateKey;
    }

    public override void EnterState()
    {
    }


    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        Context.RigidbodyFloat();
    }
}
