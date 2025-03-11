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
    [SerializeField] private PlayerCameraController _cameraController;
    
    [Space(10f)]
    [Header("Common")]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private Rigidbody _leftFootTarget;
    [SerializeField] private Rigidbody _rightFootTarget;
    [SerializeField] private Transform _leftFootRestTarget;
    [SerializeField] private Transform _rightFootRestTarget;
    
    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _acceleration = 200f;
    [SerializeField] private AnimationCurve _accelerationFactorFromDot;
    [SerializeField] private float _maxAccelForce = 100f;
    [SerializeField] private AnimationCurve _maxAccelerationForceFactorFromDot;
    [SerializeField] private Vector3 _forceScale;
    
    [Space(10f)]
    [Header("Control Variables")]
    [SerializeField] private float _springStrength = 250f;
    [SerializeField] private float _springDampener = 5f;
    
    [Space(10f)]
    [Header("Sneak Variables")]
    [SerializeField] private float _sneakSpeed = 1f;
    [SerializeField] private float _sneakStepLength = 0.38f;

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
            _leftFootTarget, _rightFootTarget, _leftFootRestTarget, _rightFootRestTarget,
            _sneakSpeed, _sneakStepLength);
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
    public Rigidbody Rigidbody => _rigidbody;
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
    
    // Movement input based on camera direction
    public Vector3 RelativeMoveInput => GetRelativeMoveInput();
    
    // Private methods
    private Vector3 GetRelativeMoveInput()
    {
        // Get the camera yaw transform
        var camera = _cameraController.GetCameraYawTransform();
        
        // Get the camera forward & right direction
        Vector3 cameraForward = GetComponent<Camera>().transform.forward;
        Vector3 cameraRight = GetComponent<Camera>().transform.right;
        
        // Get the input direction
        Vector3 inputDirection = InputManager.Instance.MoveInput.x * cameraRight + InputManager.Instance.MoveInput.z * cameraForward;
        
        // If the input direction is greater than 1, normalize it
        if (inputDirection.magnitude > 1f)
        {
            inputDirection.Normalize();
        }
        
        return inputDirection;
    }
    
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
    
    // Credit to https://youtu.be/qdskE8PJy6Q?si=hSfY9B58DNkoP-Yl
    public Vector3 MoveRigidbody(Rigidbody rigidbody, Vector3 input, Vector3 sGoalVel, Vector3 forceScale)
    {
        Vector3 move = input;

        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        // do dotproduct of input to current goal velocity to apply acceleration based on dot to direction (makes sharp turns better).
        Vector3 unitVel = sGoalVel.normalized;

        float velDot = Vector3.Dot(move, unitVel);
        float accel = _acceleration * _accelerationFactorFromDot.Evaluate(velDot);
        Vector3 goalVel = move * _maxSpeed;

        // lerp goal velocity towards new calculated goal velocity.
        sGoalVel = Vector3.MoveTowards(sGoalVel, goalVel, accel * Time.fixedDeltaTime);

        // calculate needed acceleration to reach goal velocity in a single fixed update.
        Vector3 neededAccel = (sGoalVel - rigidbody.linearVelocity) / Time.fixedDeltaTime;

        // clamp the needed acceleration to max possible acceleration.
        float maxAccel = _maxAccelForce * _maxAccelerationForceFactorFromDot.Evaluate(velDot);
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        rigidbody.AddForce(Vector3.Scale(neededAccel * rigidbody.mass, forceScale));

        // return the stored goal velocity
        return sGoalVel;
    }

    private void OnGUI()
    {
        // Create a background box for better readability
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 250, 50), "");

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
    
        GUI.Label(new Rect(20, 15, 240, 20), $"Control State: {_controlStateMachine.State}", style);
        GUI.Label(new Rect(20, 35, 240, 20), $"Sneak State: {_sneakStateMachine.State}", style);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe items
        InputManager.Instance.OnScroll -= UpdateMovementSpeed;
    }
}
