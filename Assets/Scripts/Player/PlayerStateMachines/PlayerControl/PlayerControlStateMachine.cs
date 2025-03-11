using UnityEngine;

public class PlayerControlStateMachine : StateMachine<PlayerControlStateMachine.EPlayerControlState>
{
    public enum EPlayerControlState
    {
        Grounded,
        Falling
    }

    private PlayerControlContext _context;

    // Read only properties
    public EPlayerControlState State => CurrentState.StateKey;

    private void InitializeStates()
    {
        States.Add(EPlayerControlState.Grounded, new PlayerGroundedState(_context, EPlayerControlState.Grounded));
        States.Add(EPlayerControlState.Falling, new PlayerFallingState(_context, EPlayerControlState.Falling));

        CurrentState = States[EPlayerControlState.Grounded];
    }

    public void SetContext(PlayerControlContext context)
    {
        _context = context;
        InitializeStates();
    }
}
