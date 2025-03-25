using RootMotion.Dynamics;
using UnityEngine;

public class PlayerFallingState : PlayerControlState
{
    public PlayerFallingState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        if (Context.IsGrounded() && _canGetUp)
        {
            return PlayerControlStateMachine.EPlayerControlState.Grounded;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        _timer = 0f;
        _canGetUp = false;
        Context.PuppetMaster.state = PuppetMaster.State.Dead;
    }

    public override void ExitState()
    {
        Context.PuppetMaster.state = PuppetMaster.State.Alive;
    }

    private float _timer;
    private bool _canGetUp;
    
    public override void UpdateState()
    {
        _timer += Time.deltaTime;

        if (_timer >= 5f)
        {
            _canGetUp = true;
        }
    }

    public override void FixedUpdateState()
    {
        if (!_canGetUp)
            Context.Player.Rigidbody.position = Context.PuppetMaster.transform.GetChild(0).position;
        Context.RigidbodyFloat();
    }
}
