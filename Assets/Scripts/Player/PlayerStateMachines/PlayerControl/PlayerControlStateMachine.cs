using UnityEngine;

public class PlayerControlStateMachine : StateMachine<PlayerControlStateMachine.EPlayerControlState>
{
    public enum EPlayerControlState
    {
        Grounded,
        Falling,
        Leap
    }

    private PlayerControlContext _context;

    // Read only properties
    public EPlayerControlState State => CurrentState.StateKey;

    private void InitializeStates()
    {
        States.Add(EPlayerControlState.Grounded, new PlayerGroundedState(_context, EPlayerControlState.Grounded));
        States.Add(EPlayerControlState.Falling, new PlayerFallingState(_context, EPlayerControlState.Falling));
        States.Add(EPlayerControlState.Leap, new PlayerLeapState(_context, EPlayerControlState.Leap));

        CurrentState = States[EPlayerControlState.Grounded];
    }

    public void SetContext(PlayerControlContext context)
    {
        _context = context;
        InitializeStates();
    }
}
