using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Settings")] 
    [SerializeField] private AnimationCurve _cameraStepCurve;
    [SerializeField] private float _cameraLerpSpeed = 0.1f;
    [SerializeField] private Transform _followTarget;
    [SerializeField] private float _cameraSensitivityPC = 0.03f;
    [SerializeField] private float _cameraSensitivityController = 1f;
    [SerializeField] private float _followSpeed = 0.1f;


    // Private variables
    private PlayerController _player;
    private Transform _cameraYTransform;
    private Transform _cameraXTransform;
    private Transform _cameraTransform;
    
    private float _yRotation;
    private float _xRotation;
    private float _zRotation;
    
    private void Awake()
    {
        // Initialize
        _player = GetComponentInParent<PlayerController>();
        _cameraYTransform = transform;
        _cameraXTransform = transform.GetChild(0);
        _cameraTransform = _cameraXTransform.GetChild(0);
    }

    private void Update()
    {
        UpdatePosition();
        HandleRotation();
    }

    private void UpdatePosition()
    {
        if (_player.IsGrounded)
        {
            transform.position = _followTarget.position;
            //transform.position = Vector3.Lerp(transform.position, _followTarget.position, Time.deltaTime * _followSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, _player.Position, Time.deltaTime * _followSpeed);
        }
    }

    private void HandleRotation()
    {
        // Get the camera sensitivity based on the input device
        var sensitivity = _cameraSensitivityPC;
        
        // Get the camera input
        _yRotation += InputManager.Instance.CameraInput.x * sensitivity;
        _xRotation -= InputManager.Instance.CameraInput.y * sensitivity;
        
        // Clamp the x rotation
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        
        // Get the camera z rotation based on the relative distance between the feet based on the camera forward direction
        var lFootPos = _player.LeftFootTarget.position;
        var rFootPos = _player.RightFootTarget.position;
        //lFootPos.y = 0;
        //rFootPos.y = 0;
        var feetDir = rFootPos - lFootPos;
        var feetDot = Vector3.Dot(feetDir, _cameraYTransform.forward);
        var dist = feetDir.magnitude * feetDot;
        dist = Mathf.Clamp(dist, -_player.StepLength, _player.StepLength);
        
        // Now lerp the camera z rotation based on the distance
        var wantedZ = Mathf.Lerp(-5f, 5f, _cameraStepCurve.Evaluate(dist));
        _zRotation = Mathf.Lerp(_zRotation, wantedZ, Time.deltaTime * _cameraLerpSpeed);
        
        // Rotate the camera
         _cameraYTransform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
         _cameraXTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            _cameraTransform.localRotation = Quaternion.Euler(0f, 0f, -_zRotation);
    }
    
    // Public methods
    public Transform GetCameraYawTransform()
    {
        return _cameraYTransform;
    }
    
    public float CameraX => _xRotation;
}
