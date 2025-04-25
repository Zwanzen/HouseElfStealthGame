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
    
    private void Awake()
    {
        // Initialize
        _player = GetComponentInParent<PlayerController>();
        _cameraYTransform = transform;
        _cameraXTransform = transform.GetChild(0);
        _cameraTransform = _cameraXTransform.GetChild(0);
        _camera = _cameraTransform.GetComponent<Camera>();
    }

    private void Update()
    {
        UpdatePosition();

        if(_player.IsFalling)
        {
            HandleFallingCamera();
        }
        else
        {
            HandleRotation();
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

    private void HandleFallingCamera()
    {
        var lookDirection = Quaternion.LookRotation(_player.Transform.up, Vector3.up);
        if(_player.IsStandingUp)
            lookDirection = Quaternion.LookRotation(_player.Transform.forward, Vector3.up);

        // Lerp the rotations towards the look direction
        _yRotation = Mathf.LerpAngle(_yRotation, lookDirection.eulerAngles.y, Time.deltaTime * _lerpSpeed);
        _xRotation = Mathf.LerpAngle(_xRotation, lookDirection.eulerAngles.x, Time.deltaTime * _lerpSpeed);
        _zRotation = Mathf.LerpAngle(_zRotation, 0f, Time.deltaTime * _lerpSpeed);

        // Rotate the camera
        _cameraYTransform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
        _cameraXTransform.localRotation = Quaternion.Euler(_xRotation + 20f, 0f, 0f);
        _cameraTransform.localRotation = Quaternion.Euler(0f, 0f, -_zRotation);
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

    public float CameraX => _xRotation;
}
