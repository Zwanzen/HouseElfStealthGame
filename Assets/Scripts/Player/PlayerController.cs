using System;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using static RigidbodyMovement;

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
    [SerializeField] private PlayerFootSoundPlayer _leftFootSoundPlayer;
    [SerializeField] private PlayerFootSoundPlayer _rightFootSoundPlayer;
    [SerializeField] private MovementSettings _bodyMovementSettings;
    [SerializeField] private Transform[] _limbs;
    
    [Space(10f)]
    [Header("Control Variables")]
    [SerializeField] private float _springStrength = 250f;
    [SerializeField] private float _springDampener = 5f;
    
    [Space(10f)]
    [Header("Sneak Variables")]
    [SerializeField] private float _minSneakSpeed = 1f;
    [SerializeField] private float _maxSneakSpeed = 2f;
    [SerializeField] private float _sneakStepLength = 0.38f;
    [SerializeField] private float _sneakStepHeight = 0.5f;
    [SerializeField] private float _bodyRotationSpeed = 5f;
    [FormerlySerializedAs("_sneakMovementSettings")] [SerializeField] private MovementSettings _liftedMovementSettings;
    [SerializeField] private MovementSettings _plantedMovementSettings;
    [SerializeField] private AnimationCurve _sneakSpeedCurve;

    
    private bool _isSneaking;

    // This is the maximum speed range for the player
    // Helps determine the speed state of the player
    private const int MaxSpeedRange = 15;
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
        InitializeInputEvents();
        _playerSpeedState = UpdatePlayerSpeedState();
    }

    // Initialize the state machine contexts
    private void InitializeStateMachineContexts()
    {
        _controlContext = new PlayerControlContext(this, _controlStateMachine, _rigidbody, _groundLayers,
             _leftFootTarget, _rightFootTarget, _springStrength, _springDampener);
        _sneakContext = new ProceduralSneakContext(this, _sneakStateMachine, _bodyIK, _groundLayers,
            _leftFootTarget, _rightFootTarget, _leftFootRestTarget, _rightFootRestTarget,
            _minSneakSpeed, _maxSneakSpeed, _sneakStepLength,_sneakStepHeight, _bodyRotationSpeed, _liftedMovementSettings,_plantedMovementSettings, _sneakSpeedCurve);
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
    
    // Initialize input events
    private void InitializeInputEvents()
    {
        // Subscribe to the input events
        InputManager.Instance.OnScroll += UpdateMovementSpeed;
        InputManager.Instance.OnSneakPressed += UpdateIsSneaking;
    }
    
    // Read-only properties
    /// <summary>
    /// Limbs:
    /// 0: Head, 1: Left Hand, 2: Right Hand, 3: Left Foot, 4: Right Foot, 5: Body
    /// </summary>
    public Transform[] Limbs => _limbs;
    public Rigidbody Rigidbody => _rigidbody;
    public MovementSettings BodyMovementSettings => _bodyMovementSettings;
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
    
    /// <summary>
    /// This is the current speed of the player.
    /// Used to control the speed of the player in the state machines.
    /// </summary>
    public int CurrentPlayerSpeed => _currentPlayerSpeed;
    
    // Movement input based on camera direction
    public Vector3 RelativeMoveInput => GetRelativeMoveInput();
    public PlayerCameraController Camera => _cameraController;
    
    // SOUND DEMO
    public PlayerFootSoundPlayer LeftFootSoundPlayer => _leftFootSoundPlayer;
    public PlayerFootSoundPlayer RightFootSoundPlayer => _rightFootSoundPlayer;

    // Private methods
    private Vector3 GetRelativeMoveInput()
    {
        // Get the camera yaw transform
        var cam = _cameraController.GetCameraYawTransform();
        
        // Get the camera forward & right direction
        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;
        
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
    private void UpdateMovementSpeed(int newInput = 0)
    {
        // Increment the speed based on the input
        //var newInput = InputManager.Instance.ScrollInput;
        _currentPlayerSpeed += newInput;
        
        // Clamp the speed to the max speed range
        // The player should not be able to select a zero speed
        _currentPlayerSpeed = Math.Clamp(_currentPlayerSpeed, 1, MaxSpeedRange);
        
        // If the player is sneaking, we clamp it to the sneak speed
        if (_isSneaking)
        {
            _currentPlayerSpeed = Math.Clamp(_currentPlayerSpeed, 1, 5);
        }
        
        // Update the player speed state
        _playerSpeedState = UpdatePlayerSpeedState();
    }
    
    private EPlayerSpeedState UpdatePlayerSpeedState()
    {
        // The range is split into 3 states,
        // With sneak only being active when sneak is active
    
        // Fast: 15 - 11
        // Medium: 10 - 6
        // Slow: 5 - 1

        return _currentPlayerSpeed switch
        {
            >= 11 and <= 15 => EPlayerSpeedState.Fast,
            >= 6 and <= 10 => EPlayerSpeedState.Medium,
            >= 1 and <= 5 => EPlayerSpeedState.Slow,
            _ => EPlayerSpeedState.Slow // Default case
        };
    }

    private void UpdateIsSneaking(bool updated)
    {
        // Toggle the sneaking input
        if (_isSneaking && updated == true)
        {
            _isSneaking = false;
        }
        else if (!_isSneaking && updated == true)
        {
            _isSneaking = true;
        }
        else if (updated == false)
        {
            _isSneaking = false;
        }
        
        // We need to update the player speed state
        // to make sure the correct speed is set
        UpdateMovementSpeed();
    }

    private void OnGUI()
    {
        // Create a background box for better readability
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 250, 100), "");

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
    
        GUI.Label(new Rect(20, 15, 240, 20), $"Control State: {_controlStateMachine.State}", style);
        GUI.Label(new Rect(20, 35, 240, 20), $"Sneak State: {_sneakStateMachine.State}", style);
        GUI.Label(new Rect(20, 55, 240, 20), $"Player Is Sneaking: {_isSneaking}", style);
        GUI.Label(new Rect(20, 75, 240, 20), $"Player Speed State: {_playerSpeedState}", style);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe items
        InputManager.Instance.OnScroll -= UpdateMovementSpeed;
    }
}
