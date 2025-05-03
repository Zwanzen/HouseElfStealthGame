using UnityEngine;

public class PlayerLeapState : PlayerControlState
{
    public PlayerLeapState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        if (Context.ShouldFall)
            return PlayerControlStateMachine.EPlayerControlState.Falling;

        if(!Context.ShouldLeap)
            return PlayerControlStateMachine.EPlayerControlState.Grounded;

        return StateKey;
    }

    public override void EnterState()
    {
        Context.Player.Animator.SetAnim(PlayerAnimator.EAnimType.Jump);
    }

    public override void ExitState()
    {
        Context.Player.Animator.SetAnim(PlayerAnimator.EAnimType.Land);
        Context.Player.Camera.Stumble();
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        var hipPosOffset = Context.BetweenFeet(0.5f);
        hipPosOffset.y = 0;

        Context.HandleStretch();

        Context.MoveToHipPoint(Context.CalculatePelvisPoint(hipPosOffset));

        var velDir = Context.Player.Rigidbody.linearVelocity;
        velDir.y = 0f;
        velDir.Normalize();

        if (velDir != Vector3.zero)
            Context.UpdateBodyRotation(velDir);
    }
}
