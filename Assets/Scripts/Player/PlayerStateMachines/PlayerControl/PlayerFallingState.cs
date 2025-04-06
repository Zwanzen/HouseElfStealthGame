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
        Context.Player.SetPlayerStumble(false);
    }

    public override void ExitState()
    {
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
        {
            var rotation = Quaternion.LookRotation(Context.Player.Camera.GetCameraYawTransform().forward, Vector3.up);
            Context.Player.Rigidbody.transform.rotation = rotation;
        }
        Context.RigidbodyFloat();
    }
}
