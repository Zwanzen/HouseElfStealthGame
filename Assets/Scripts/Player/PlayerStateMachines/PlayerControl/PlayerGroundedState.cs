using Unity.Mathematics;
using UnityEngine;

public class PlayerGroundedState : PlayerControlState
{
    public PlayerGroundedState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }


    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        var bothFeetFall = Context.LeftFoot.SM.State == FootControlStateMachine.EFootState.Falling &&
                           Context.RightFoot.SM.State == FootControlStateMachine.EFootState.Falling;  
        if (bothFeetFall)
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

    private float _timer;
    public override void UpdateState()
    {

    }

    public override void FixedUpdateState()
    {
        Context.RigidbodyFloat();
        Context.MoveBody(Context.BetweenFeet(Context.FeetLerp()));
        
        if(Context.Player.RelativeMoveInput != Vector3.zero)
            Context.UpdateBodyRotation(Context.Player.Camera.GetCameraYawTransform().forward);
    }
}
