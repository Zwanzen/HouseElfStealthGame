using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Settings")] 
    [SerializeField] private AnimationCurve _cameraStepCurve;
    [SerializeField] private float _cameraLerpSpeed = 0.1f;
    [SerializeField] private Transform _followTarget;
    [SerializeField] private float _cameraSensitivityPC = 0.03f;
    [SerializeField] private float _cameraSensitivityController = 1f;
    [SerializeField] private float _followSpeed = 0.1f;
    [SerializeField] private float _lerpSpeed = 10f;


    // Private variables
    private PlayerController _player;
    private AimIK _aimIK;
    private Camera _camera;
    private Transform _cameraYTransform;
    private Transform _cameraXTransform;
    private Transform _cameraTransform;
    
    private float _yRotation;
    private float _xRotation;
    private float _zRotation;
    
    private float _minFov = 80f;
    
    // Properties
    public float LookDownLerp => _xRotation / 60f;

    public float LookDownLerpOffset(float offset)
    {
        return (_xRotation - offset) / (60f - offset);
    }
    
    private void Initialize()
    {
        transform.SetParent(null);
        _player = PlayerController.Instance;
        _followTarget = _player.Eyes;
        _cameraYTransform = transform;
        _cameraXTransform = transform.GetChild(0);
        _cameraTransform = _cameraXTransform.GetChild(0);
        _camera = _cameraTransform.GetComponent<Camera>();
    }

    private void Start()
    {
        Initialize();
        _aimIK = _player.AimIK;

        _player.OnFall += OnFall;
        _player.OnStopFall += OnStopFall;
    }

    private void Update()
    {

        if(!_player.IsFalling)
        {
            HandleRotation();
            UpdatePosition();
            UpdateAimSolverWeight(Time.deltaTime * 0.5f);
        }
        else
        {
            UpdateAimSolverWeight(-Time.deltaTime * 0.5f);
            HandleFallCameraRotation();
        }
    }

    private void UpdatePosition()
    {
        transform.position = _followTarget.position;

        // if camera fov is not 90, lerp it to 90
        if (!Mathf.Approximately(_camera.fieldOfView, 90))
        {
            _timer += Time.deltaTime * 2f;
            _camera.fieldOfView = Mathf.Lerp(_minFov, 90f, _timer);
        }
    }

    private void HandleRotation()
    {
        _aimIK.solver.IKPositionWeight = 1f;

        // Get the camera sensitivity based on the input device
        var sensitivity = _cameraSensitivityPC;
        
        // Get the camera input with a small dead zone to prevent tiny movements
        float xInput = InputManager.Instance.CameraInput.x;
        float yInput = InputManager.Instance.CameraInput.y;
        
        // Apply input to rotation values
        _yRotation += xInput * sensitivity;
        _xRotation -= yInput * sensitivity;
        
        // Normalize Y rotation to keep it between 0 and 360
        if (_yRotation > 360f) _yRotation -= 360f;
        if (_yRotation < 0f) _yRotation += 360f;
        
        // Add extra constraining near poles to prevent gimbal lock effect
        float xClampLimit = 85f; // slightly reduced from 90
        _xRotation = Mathf.Clamp(_xRotation, -xClampLimit, xClampLimit);
        
        // Get the camera z rotation based on the relative distance between the feet based on the camera forward direction
        var lFootPos = _player.LeftFootTarget.position;
        var rFootPos = _player.RightFootTarget.position;
        var feetDir = rFootPos - lFootPos;
        
        // Ensure we're using the correctly oriented forward vector
        var forwardVector = _cameraYTransform.forward;
        var feetDot = Vector3.Dot(feetDir, forwardVector);
        var dist = feetDir.magnitude * feetDot;
        dist = Mathf.Clamp(dist, -_player.StepLength, _player.StepLength);
        
        // Now lerp the camera z rotation based on the distance
        var wantedZ = Mathf.Lerp(-5f, 5f, _cameraStepCurve.Evaluate(dist));
        _zRotation = Mathf.Lerp(_zRotation, wantedZ, Time.deltaTime * _cameraLerpSpeed);
        
        // Apply rotations in the correct order to avoid gimbal lock
        _cameraYTransform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
        _cameraXTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        _cameraTransform.localRotation = Quaternion.Euler(0f, 0f, -_zRotation);
    }

    private void HandleFallCameraRotation()
    {
        // Slerp all the camera rotations to local zero
        _cameraYTransform.localRotation = Quaternion.Slerp(_cameraYTransform.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime * _followSpeed);
        _cameraXTransform.localRotation = Quaternion.Slerp(_cameraXTransform.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime * _followSpeed);
        _cameraTransform.localRotation = Quaternion.Slerp(_cameraTransform.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime * _followSpeed);
    }

    private void OnFall()
    {
        _aimIK.solver.IKPositionWeight = 0f;

        // Make this transform the child of the follow target
        transform.SetParent(_followTarget);

    }

    private void OnStopFall()
    {
        _aimIK.solver.IKPositionWeight = 0f;

        // Make this transform the child of the follow target
        transform.SetParent(_player.transform);

        // Store the camera rotations
        _yRotation = _cameraYTransform.localEulerAngles.y;
        _xRotation = _cameraXTransform.localEulerAngles.x;
        _zRotation = _cameraTransform.localEulerAngles.z;
    }

    private void UpdateAimSolverWeight(float amount)
    {
        _aimIK.solver.IKPositionWeight += amount;
    }




    // Public methods
    public Transform GetCameraYawTransform()
    {
        return _cameraYTransform;
    }

    private float _timer;

    public void Stumble()
    {
        // set the camera fov to 60
        _camera.fieldOfView = _minFov;
        _timer = 0f;
    }

    public void TeleportToFollowTarget()
    {
        transform.position = _followTarget.position;
    }

    public float CameraX => _xRotation;

    private void OnDestroy()
    {
        // Unsubscribe from events
        _player.OnFall -= OnFall;
        _player.OnStopFall -= OnStopFall;
    }
}
