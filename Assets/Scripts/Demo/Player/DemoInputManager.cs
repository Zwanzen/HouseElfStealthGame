using System;
using UnityEngine;

public class DemoInputManager : MonoBehaviour
{

    private DemoActions _demoActions;
    
    private Vector2 _moveInput;
    private Vector2 _cameraInput;
    
    // Read-only properties
    public Vector3 MoveInput => new Vector3(_moveInput.x, 0, _moveInput.y);
    public Vector2 CameraInput => _cameraInput;

    private void OnEnable()
    {
        if (_demoActions == null)
        {
            _demoActions = new DemoActions();

            _demoActions.Player.Move.performed += _demoActions => _moveInput = _demoActions.ReadValue<Vector2>();
            _demoActions.Player.Look.performed += i => _cameraInput = i.ReadValue<Vector2>();
        }

        _demoActions.Enable();
    }
    
    private void OnDisable()
    {
        _demoActions.Disable();
    }
}
