using UnityEngine;

public class PlayerGroundedState : PlayerControlState
{
    public PlayerGroundedState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        if (!Context.IsGrounded())
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
