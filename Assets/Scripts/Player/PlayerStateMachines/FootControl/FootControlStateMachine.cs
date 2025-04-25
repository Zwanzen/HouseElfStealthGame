public class FootControlStateMachine : StateMachine<FootControlStateMachine.EFootState>
{
    public enum EFootState
    {
        Idle, // Used when not in use, no IKs are applied.
        Start, // Turns on the IKs and sets the foot to planted.
        Stop, // Turns off the IKs and sets the foot to idle.
        Falling, // Used when the foot is falling, and should be placed.
        Planted, // Tries to keep the foot planted, and decides if it should be lifted.
        Lifted, // Lifts the foot and moves it based on input and limits.
        Placing, // Tried to place the foot, if failed, should fall.
    }
    
    private FootControlContext _context;

    // Read only properties
    public EFootState State => CurrentState.StateKey;

    private void InitializeStates()
    {
        States.Add(EFootState.Idle, new FootIdleState(_context, EFootState.Idle));
        States.Add(EFootState.Start, new FootStartState(_context, EFootState.Start));
        States.Add(EFootState.Stop, new FootStopState(_context, EFootState.Stop));
        States.Add(EFootState.Falling, new FootFallingState(_context, EFootState.Falling));
        States.Add(EFootState.Planted, new FootPlantedState(_context, EFootState.Planted));
        States.Add(EFootState.Lifted, new FootLiftedState(_context, EFootState.Lifted));
        States.Add(EFootState.Placing, new FootPlacingState(_context, EFootState.Placing));
        
        // Set the initial state
        CurrentState = States[EFootState.Start];
    }

    /// <summary>
    /// This method controls when the states are being initialized.
    /// </summary>
    public void SetContext(FootControlContext context)
    {
        _context = context;
        InitializeStates();
    }
}