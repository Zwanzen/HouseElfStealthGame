using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using RootMotion.Dynamics;
using RootMotion.FinalIK;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static RigidbodyMovement;

public class PlayerController : MonoBehaviour
{

    // State Machines
    private PlayerControlStateMachine _controlStateMachine;
    private FootControlStateMachine _leftFootStateMachine;
    private FootControlStateMachine _rightFootStateMachine;
    
    // State Machine Contexts
    private PlayerControlContext _controlContext;
    private FootControlContext _leftFootContext;
    private FootControlContext _rightFootContext;
    
    // Private variables
    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private FullBodyBipedIK _bodyIK;
    [SerializeField] private Animator _anim;
    [SerializeField] private PlayerCameraController _cameraController;

    [Space(10f)]
    [Header("Common")]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private Transform[] _limbs;

    [Space(10f)]
    [Header("Body Variables")]
    [SerializeField] private float _bodyRotationSpeed = 5f;
    [SerializeField] private CapsuleCollider _bodyCollider;
    [SerializeField] private SphereCollider _fallCollider;

    [Space(10f)] 
    [Header("Foot Control")] 
    [SerializeField] private FootRef _leftFootRef;
    [Space(10f)]
    [SerializeField] private FootRef _rightFootRef;
    [Space(10f)]
    [SerializeField] private float _stepLength = 0.5f;
    [SerializeField] private float _stepHeight = 0.5f;
    [SerializeField] private AnimationCurve _speedCurve;
    [SerializeField] private AnimationCurve _heightCurve;
    [SerializeField] private AnimationCurve _placeSpeedCurve;
    [SerializeField] private AnimationCurve _offsetCurve;
    
    [Space(10f)]
    [Header("Movement Settings")]
    [SerializeField] private MovementSettings _bodyMovementSetting;
    [SerializeField] private MovementSettings _minSneakSetting;
    [SerializeField] private MovementSettings _maxSneakSetting;
    [SerializeField] private MovementSettings _minWalkSetting;
    [SerializeField] private MovementSettings _maxWalkSetting;

    [SerializeField] private MovementSettings _placeSettings;

    [Space(10f)] [Header("Temp")] 
    [SerializeField] private RectTransform _leftClick;
    [SerializeField] private RectTransform _rightClick;
    [SerializeField] private float MinImageScale = 0.5f;
    [SerializeField] private float MaxImageScale = 1.5f;
    [SerializeField] private AnimationCurve _imageScaleCurve;
    [SerializeField] private Image _leftImage;
    [SerializeField] private Image _rightImage;
    [SerializeField] private Slider _movementSpeedSlider;

    // Private variables
    private Foot leftFoot;
    private Foot rightFoot;

    private float _wantedLScale;
    private float _wantedRScale;
    
    private Color _wantedLColor;
    private Color _wantedRColor;
    
    private bool _isSneaking;

    // This is the maximum speed range for the player
    // Helps determine the speed state of the player
    [HideInInspector]
    public const int MaxSpeedRange = 10;
    private int _currentPlayerSpeed = 5;
    
    public enum EPlayerSpeedState
    {
        Fast,
        Medium,
        Slow,
        Sneak
    }

    // Singleton instance
    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InitializeStateMachines();
        
        // setting private variables
        Transform = _rigidbody.transform;
        Animator = new PlayerAnimator(this, _anim);
        AimIK = _bodyIK.GetComponent<AimIK>();
    }

    private void Start()
    {
        InitializeInputEvents();
        UpdateMovementSpeedSlider();
    }

    private void Update()
    {
        HandleStepUI();
        Animator.Update();
    }

    private void UpdateMovementSpeedSlider()
    {
        if(!_movementSpeedSlider)
            return;

        // Update the movement speed slider value
        _movementSpeedSlider.value = _currentPlayerSpeed;
    }

    private void HandleStepUI()
    {
        var lFootPos = LeftFootTarget.position;
        var rFootPos = RightFootTarget.position;
        
        var feetDir = rFootPos - lFootPos;
        var relativeDir = _cameraController.GetCameraYawTransform().forward;
        if(RelativeMoveInput.magnitude > 0)
            relativeDir = RelativeMoveInput;
        var feetDot = Vector3.Dot(feetDir, relativeDir);
        var dist = feetDir.magnitude * feetDot;
        dist = Mathf.Clamp(dist, -_stepLength, _stepLength);

        var lerp = _imageScaleCurve.Evaluate(dist);
        
        // Downwards lerp
        var angle = _cameraController.CameraX - 40f;
        var angleValue = Mathf.Lerp(1, 0, angle/20f);
        
        _wantedLScale = Mathf.Lerp(MinImageScale, MaxImageScale, lerp) * angleValue;
        _wantedRScale = Mathf.Lerp(MaxImageScale, MinImageScale, lerp) * angleValue;
        
        var maxColor = new Color(255, 255, 255, 0.5f * angleValue);
        var minColor = new Color(255, 255, 255, 0f);
        
        _wantedLColor = Color.Lerp(minColor, maxColor, lerp);
        _wantedRColor = Color.Lerp(maxColor, minColor, lerp);
        
        var lerpSpeed = 5f;
        
        // Lerp the scale to the wanted scale
        _leftClick.localScale = Vector3.Lerp(_leftClick.localScale, Vector3.one * _wantedLScale, Time.deltaTime * lerpSpeed);
        _rightClick.localScale = Vector3.Lerp(_rightClick.localScale, Vector3.one * _wantedRScale, Time.deltaTime * lerpSpeed);
        
        // Lerp the color to the wanted color
        _leftImage.color = Color.Lerp(_leftImage.color, _wantedLColor, Time.deltaTime * lerpSpeed * 10f);
        _rightImage.color = Color.Lerp(_rightImage.color, _wantedRColor, Time.deltaTime * lerpSpeed * 10f);
    }

    private void InitializeStateMachines()
    {
        // Add the state machines to the player controller
        _controlStateMachine = this.AddComponent<PlayerControlStateMachine>();
        _leftFootStateMachine = this.AddComponent<FootControlStateMachine>();
        _rightFootStateMachine = this.AddComponent<FootControlStateMachine>();
        
        // Then we construct the feet.
        // The feet need references to their state machine.
        leftFoot = new Foot(_leftFootRef, _leftFootStateMachine);
        rightFoot = new Foot(_rightFootRef, _rightFootStateMachine);
        
        // Then we construct the context files after the feet are created.
        _controlContext = new PlayerControlContext(this, _bodyIK, _bodyCollider, _fallCollider, _groundLayers,
            leftFoot, rightFoot, _bodyMovementSetting, _stepLength, _stepHeight);
        
        _leftFootContext = new FootControlContext(this, _bodyIK, _groundLayers,
            leftFoot, rightFoot, _stepLength, _stepHeight, _minSneakSetting, _maxSneakSetting, _minWalkSetting, _maxWalkSetting, 
            _placeSettings, _speedCurve, _heightCurve, _placeSpeedCurve, _offsetCurve);
        _rightFootContext = new FootControlContext(this, _bodyIK, _groundLayers,
            rightFoot, leftFoot, _stepLength, _stepHeight, _minSneakSetting, _maxSneakSetting, _minWalkSetting, _maxWalkSetting, _placeSettings, 
            _speedCurve, _heightCurve, _placeSpeedCurve, _offsetCurve);
        
        // Set the context for the state machines
        // This also initializes the states within the state machines
        _controlStateMachine.SetContext(_controlContext);
        _leftFootStateMachine.SetContext(_leftFootContext);
        _rightFootStateMachine.SetContext(_rightFootContext);
    }
    
    // Initialize input events
    private void InitializeInputEvents()
    {
        // Subscribe to the input events
        InputManager.Instance.OnScroll += UpdateMovementSpeed;
        InputManager.Instance.OnToggleMovement += UpdateIsSneaking;
    }

    // Events
    public event Action OnFall;
    public event Action OnStopFall;

    // Read-only properties
    /// <summary>
    /// Limbs:
    /// 0: Head, 1: Left Hand, 2: Right Hand, 3: Left Foot, 4: Right Foot, 5: Body
    /// </summary>
    public Transform[] Limbs => _limbs;
    public Rigidbody Rigidbody => _rigidbody;
    public Transform Transform { get; private set; }
    public AimIK AimIK { get; private set; }

    /// <summary>
    /// This is used to control the player's animations.
    /// </summary>
    public PlayerAnimator Animator { get; private set; }

    // Sneaking Properties
    public Rigidbody LeftFootTarget => _leftFootRef.Target;
    public Rigidbody RightFootTarget => _rightFootRef.Target;
    public MovementSettings BodyMovementSettings => _bodyMovementSetting;
    public static float Height => 1.0f;
    public float StepLength => _stepLength;

    /// <summary>
    /// The player's offset position from the rigidbody's position, adjusted by the character height.
    /// The player rigidbody is actually at the feet of the player, so we need to add the character height to the position.
    /// </summary>
    public Vector3 Position => _rigidbody.position; 
    /// <summary>
    /// This is the new player position^
    /// </summary>
    public Vector3 EyePosition => _limbs[0].position;
    
    /// <summary>
    /// The threshold for the height difference to consider the player grounded.
    /// </summary>
    public static float HeightThreshold => 0.07f;

    /// <summary>
    /// If the player is in the grounded state.
    /// </summary>
    public bool IsGrounded => _controlStateMachine.State == PlayerControlStateMachine.EPlayerControlState.Grounded;

    /// <summary>
    /// If the player is in the falling state.
    /// </summary>
    public bool IsFalling => _controlStateMachine.State == PlayerControlStateMachine.EPlayerControlState.Falling;
    public bool IsStandingUp { get; private set; } = false;

    /// <summary>
    /// If the player tries to place feet and there is no ground.
    /// </summary>
    public bool IsStumble { get; private set; }
    
    public bool IsSneaking => _isSneaking;

    public bool IsJumping { get; private set; }

    public bool IsMoving => InputManager.Instance.MoveInput != Vector3.zero;
    
    /// <summary>
    /// This is the current lerped speed of the player.
    /// Used to control the speed of the player in the state machines.
    /// </summary>
    public float CurrentPlayerSpeed => ((float)_currentPlayerSpeed / (float)MaxSpeedRange);
    
    // Movement input based on camera direction
    public Vector3 RelativeMoveInput => GetRelativeMoveInput();
    public PlayerCameraController Camera => _cameraController;
    
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

        // Update the visual slider UI
        UpdateMovementSpeedSlider();
    }

    private void UpdateIsSneaking()
    {
        // Toggle sneaking
        _isSneaking = !_isSneaking;
    }

    // Public methods
    public void SetJump(bool state)
    {
        IsJumping = state;
    }

    public void SetStandingUp(bool state)
    {
        IsStandingUp = state;
    }

    public void StartFall()
    {
        OnFall.Invoke();
    }

    public void StopFall()
    {
        OnStopFall.Invoke();
    }

    public void Teleport(Vector3 position, Vector3 direction)
    {
        // Teleport the player to the new position
        _rigidbody.isKinematic = true;
        _rigidbody.position = position;
        _rigidbody.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Stops the feet, then after teleport frame, we start the feet again
        leftFoot.StopFoot();
        rightFoot.StopFoot();

        // Reset the velocity of the player
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        StartCoroutine(ExecuteAfterTeleport());
    }

    private IEnumerator ExecuteAfterTeleport()
    {
        // wait to next frame
        yield return new WaitForSeconds(0.1f);

        _rigidbody.isKinematic = false;
        leftFoot.StartFoot();
        rightFoot.StartFoot();
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
        GUI.Label(new Rect(20, 35, 240, 20), $"Left Foot State: {_leftFootStateMachine.State}", style);
        GUI.Label(new Rect(20, 55, 240, 20), $"Right Foot State: {_rightFootStateMachine.State}", style);
        GUI.Label(new Rect(20, 75, 240, 20), $"Player Sneak: {_isSneaking}", style);
        GUI.Label(new Rect(20, 95, 240, 20), $"Player Speed: {_currentPlayerSpeed}", style);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe items
        InputManager.Instance.OnScroll -= UpdateMovementSpeed;
    }
}
