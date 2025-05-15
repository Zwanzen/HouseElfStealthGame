using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    // Singleton
    public static InputManager Instance;
    
    // Private
    private InputSystem_Actions _inputActions;

    private Vector2 _moveInput;
    private Vector2 _cameraInput;
    private float _scrollInput;
    
    private bool _isHoldingLMB;
    private bool _isHoldingRMB;


    // Read-only
    public Vector3 MoveInput => new Vector3(_moveInput.x, 0f, _moveInput.y);
    public Vector2 CameraInput => _cameraInput;
    public int ScrollInput => (int)_scrollInput;
    public bool IsHoldingLMB => _isHoldingLMB;
    public bool IsHoldingRMB => _isHoldingRMB;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    // Event used to update the player speed state more efficiently
    // This is called twice, but doesn't seem like it can double the value
    public event Action<int> OnScroll;

    // Event for interact attempt
    public event Action OnInteract;
    
    private void OnLMB(InputAction.CallbackContext context)
    {
        _isHoldingLMB = context.ReadValueAsButton();
    }
    
    private void OnRMB(InputAction.CallbackContext context)
    {
        _isHoldingRMB = context.ReadValueAsButton();
    }

    /// <summary>
    /// Disables the input actions, feks when the game is paused.
    /// </summary>
    public void DisableInputs()
    {
        // Reset the input values
        _moveInput = Vector2.zero;
        _cameraInput = Vector2.zero;
        _inputActions.Disable();
    }

    /// <summary>
    /// Enables the input actions, feks when the game is unpaused.
    /// </summary>
    public void EnableInputs()
    {
        _inputActions.Enable();
    }

    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new InputSystem_Actions();
            
            _inputActions.Player.Move.performed += playerControls => _moveInput = playerControls.ReadValue<Vector2>();
            _inputActions.Player.Look.performed += i => _cameraInput = i.ReadValue<Vector2>();
            _inputActions.Player.Scroll.performed += i => OnScroll?.Invoke((int)i.ReadValue<float>());
            
            _inputActions.Player.LeftFoot.performed += OnLMB;
            _inputActions.Player.LeftFoot.canceled += OnLMB;
            _inputActions.Player.RightFoot.performed += OnRMB;
            _inputActions.Player.RightFoot.canceled += OnRMB;
            
            _inputActions.Player.Interact.performed += i => OnInteract?.Invoke();
        }

        _inputActions.Enable();
    }

    private void OnDestroy()
    {
        if (_inputActions != null)
            _inputActions.Disable();
    }

    private void OnDisable()
    {
        if (_inputActions != null)
            _inputActions.Disable();
    }
}
