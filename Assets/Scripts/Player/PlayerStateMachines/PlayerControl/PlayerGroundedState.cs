using UnityEngine;

public class PlayerGroundedState : PlayerControlState
{
    public PlayerGroundedState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    private bool _shouldFall;
    private const float FallTimeThreshold = 0.15f;

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        var dist = Vector3.Distance(Context.LeftFootTarget.position, Context.RightFootTarget.position);
        
        if (_shouldFall || dist > 2f || Context.Player.IsStumble)
        {
            return PlayerControlStateMachine.EPlayerControlState.Falling;
        }


        return StateKey;
    }

    public override void EnterState()
    {
        _shouldFall = false;
        _timer = FallTimeThreshold;
    }


    public override void ExitState()
    {

    }

    private float _timer;
    public override void UpdateState()
    {
        if (!Context.IsGrounded())
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _shouldFall = true;
            }
        }else
        {
            _timer = FallTimeThreshold;
        }
    }

    public override void FixedUpdateState()
    {
        Context.RigidbodyFloat();
    }
}
