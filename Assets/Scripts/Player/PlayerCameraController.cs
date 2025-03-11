using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Settings")] 
    [SerializeField] private Transform _followTarget;
    [SerializeField] private float _cameraSensitivity = 0.03f;


    // Private variables
    private PlayerController _player;
    private Transform _cameraYTransform;
    private Transform _cameraXTransform;
    
    private float _yRotation;
    private float _xRotation;
    
    private void Awake()
    {
        // Initialize
        _player = GetComponentInParent<PlayerController>();
        _cameraYTransform = transform;
        _cameraXTransform = transform.GetChild(0);
        
        // Set the initial rotation
        _yRotation = _player.transform.eulerAngles.y;
    }

    private void Update()
    {
        UpdatePosition();
        HandleRotation();
    }

    private void UpdatePosition()
    {
        transform.position = _followTarget.position;
    }

    private void HandleRotation()
    {
        // Get the camera input
        _yRotation += InputManager.Instance.CameraInput.x * _cameraSensitivity;
        _xRotation -= InputManager.Instance.CameraInput.y * _cameraSensitivity;
        
        // Clamp the x rotation
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        
        // Rotate the camera
        _cameraYTransform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
        _cameraXTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }
}
