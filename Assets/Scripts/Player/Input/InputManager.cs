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

    private bool _isLifting;


    // Read-only
    public Vector3 MoveInput => new Vector3(_moveInput.x, 0f, _moveInput.y);
    public Vector2 CameraInput => _cameraInput;
    public int ScrollInput => (int)_scrollInput;
    public bool IsHoldingLMB => _isHoldingLMB;
    public bool IsHoldingRMB => _isHoldingRMB;
    public bool IsLifting => _isLifting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        
        SceneManager.activeSceneChanged += OnSceneChange;
    }
    
    // Event used to update the player speed state more efficiently
    // This is called twice, but doesn't seem like it can double the value
    public event Action<int> OnScroll;
    
    // Event to toggle sneaking
    public event Action<bool> OnSneakPressed;
    
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

    private void OnLift(InputAction.CallbackContext context)
    {
        _isLifting = context.ReadValueAsButton();
    }
    
    private void OnSceneChange(Scene oldScene, Scene newScene)
    {
        // If we are loading into our world scene, enable 
        //if(newScene.buildIndex ==)
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
            
            _inputActions.Player.Sneak.performed += i => OnSneakPressed?.Invoke(i.ReadValueAsButton());

            _inputActions.Player.Lift.performed += OnLift;
            _inputActions.Player.Lift.canceled += OnLift;
            
            _inputActions.Player.Interact.performed += i => OnInteract?.Invoke();
        }

        _inputActions.Enable();
    }

    private void OnDestroy()
    {
        // If we destroy the object, we need to unsubscribe from the events
        SceneManager.activeSceneChanged -= OnSceneChange;
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }
}
