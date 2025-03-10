using UnityEngine;

public abstract class PlayerControlState : BaseState<PlayerControlStateMachine.EPlayerControlState>
{
    protected PlayerControlContext Context;

    public PlayerControlState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(key)
    {
        Context = context;
    }
}
