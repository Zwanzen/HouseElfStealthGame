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

        Context.UpdateBodyRotation(Context.Player.Camera.GetCameraYawTransform().forward);
    }
}
