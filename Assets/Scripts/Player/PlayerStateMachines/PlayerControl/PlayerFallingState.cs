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
        var bothFeetFall = Context.LeftFoot.SM.State == FootControlStateMachine.EFootState.Falling &&
                           Context.RightFoot.SM.State == FootControlStateMachine.EFootState.Falling;
        if (!bothFeetFall)
            return PlayerControlStateMachine.EPlayerControlState.Grounded;
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
        Context.RigidbodyFloat();
    }
}
