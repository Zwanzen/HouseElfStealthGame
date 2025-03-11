using System;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    // State Machines
    private PlayerControlStateMachine _controlStateMachine;
    private ProceduralSneakStateMachine _sneakStateMachine;
    
    // State Machine Contexts
    private PlayerControlContext _controlContext;
    private ProceduralSneakContext _sneakContext;
    
    // Private variables
    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private FullBodyBipedIK _bodyIK;
    
    [Space(10f)]
    [Header("Common")]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private Transform _leftFootTarget;
    [SerializeField] private Transform _rightFootTarget;
    [SerializeField] private Transform _leftFootRestTarget;
    [SerializeField] private Transform _rightFootRestTarget;
    
    [Space(10f)]
    [Header("Control Variables")]
    [SerializeField] private float _springStrength = 250f;
    [SerializeField] private float _springDampener = 5f;
    
    [Space(10f)]
    [Header("Sneak Variables")]
    [SerializeField] private float _sneakSpeed = 1f;

    // This is the maximum speed range for the player
    // Helps determine the speed state of the player
    private const int MaxSpeedRange = 12;
    private int _currentPlayerSpeed = 5;
    private EPlayerSpeedState _playerSpeedState;
    
    public enum EPlayerSpeedState
    {
        Fast,
        Medium,
        Slow,
        Sneak
    }

    private void Awake()
    {
        // Initialize the state machine contexts first, SMs depend on them
        InitializeStateMachineContexts();
        InitializeStateMachines();
    }

    private void Start()
    {
        InputManager.Instance.OnScroll += UpdateMovementSpeed;
        _playerSpeedState = UpdatePlayerSpeedState();
    }

    // Initialize the state machine contexts
    private void InitializeStateMachineContexts()
    {
        _controlContext = new PlayerControlContext(this, _controlStateMachine, _rigidbody, _groundLayers,
            _springStrength, _springDampener);
        _sneakContext = new ProceduralSneakContext(this, _sneakStateMachine, _bodyIK, _groundLayers,
            _leftFootTarget, _rightFootTarget, _leftFootRestTarget, _rightFootRestTarget);
    }
    

    // Adding and initializing the state machines with contexts
    private void InitializeStateMachines()
    {
        // Add the state machines to the player controller
        _controlStateMachine = this.AddComponent<PlayerControlStateMachine>();
        _sneakStateMachine = this.AddComponent<ProceduralSneakStateMachine>();
        
        // Set the context for the state machines
        _controlStateMachine.SetContext(_controlContext);
        _sneakStateMachine.SetContext(_sneakContext);
    }
    
    // Read-only properties
    public static float Height => 1.0f;
    
    /// <summary>
    /// The player's offset position from the rigidbody's position, adjusted by the character height.
    /// The player rigidbody is actually at the feet of the player, so we need to add the character height to the position.
    /// </summary>
    public Vector3 Position => _rigidbody.position + new Vector3(0, Height, 0); 
    
    /// <summary>
    /// The threshold for the height difference to consider the player grounded.
    /// </summary>
    public static float HeightThreshold => 0.07f;
    
    /// <summary>
    /// If the player's control state is grounded.
    /// </summary>
    public bool IsGrounded => _controlStateMachine.State == PlayerControlStateMachine.EPlayerControlState.Grounded;
    
    /// <summary>
    /// This is the control state of the player based on the selected speed.
    /// </summary>
    public EPlayerSpeedState PlayerSpeedState => _playerSpeedState;
    
    // Private methods
    
    // Calculates the current selected speed increment
    private void UpdateMovementSpeed(int newInput)
    {
        // Increment the speed based on the input
        //var newInput = InputManager.Instance.ScrollInput;
        _currentPlayerSpeed += newInput;
        
        // Clamp the speed to the max speed range
        // The player should not be able to select a zero speed
        _currentPlayerSpeed = Math.Clamp(_currentPlayerSpeed, 1, MaxSpeedRange);
        
        // Update the player speed state
        _playerSpeedState = UpdatePlayerSpeedState();
        Debug.Log(_playerSpeedState.ToString());
    }
    
    // Public Methods
    private EPlayerSpeedState UpdatePlayerSpeedState()
    {
        // The range is split into 4 states
        // Fast: 12 - 10
        // Medium: 9 - 7
        // Slow: 6 - 4
        // Sneak: 3 - 1

        return _currentPlayerSpeed switch
        {
            >= 10 and <= 12 => EPlayerSpeedState.Fast,
            >= 7 and <= 9 => EPlayerSpeedState.Medium,
            >= 4 and <= 6 => EPlayerSpeedState.Slow,
            _ => EPlayerSpeedState.Sneak
        };
    }

    private void OnDestroy()
    {
        // Unsubscribe items
        InputManager.Instance.OnScroll -= UpdateMovementSpeed;
    }
}
