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


    // Read-only
    public Vector2 MoveInput => _moveInput;
    public Vector2 CameraInput => _cameraInput;

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
        
        SceneManager.activeSceneChanged += OnSceneChange;
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
