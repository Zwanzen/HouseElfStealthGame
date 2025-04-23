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
        if(Input.GetKeyDown(KeyCode.H))
            Context.StopFeet();
        if(Input.GetKeyDown(KeyCode.J))
            Context.StartFeet();
    }

    public override void FixedUpdateState()
    {
        Context.RigidbodyFloat();
        Context.MoveBody(Context.BetweenFeet(Context.FeetLerp()));
        
        if(Context.Player.RelativeMoveInput != Vector3.zero)
            Context.UpdateBodyRotation(Context.Player.Camera.GetCameraYawTransform().forward);
    }
}
