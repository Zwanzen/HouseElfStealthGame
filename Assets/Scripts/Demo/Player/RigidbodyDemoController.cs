using System;
using UnityEngine;

public class RigidbodyDemoController : MonoBehaviour
{
    // Singeton instance
    public static RigidbodyDemoController Instance;
    
    // Private variables
    [Header("Reference")]
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private DemoInputManager _input;

    [SerializeField] private Transform _head;
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _rightHand;
    [SerializeField] private Transform _leftLeg;
    [SerializeField] private Transform _rightLeg;
    [SerializeField] private Transform _body;
    
    private Rigidbody _rigidbody;
    private Transform _cameraTransform;
    
    [Space(10)]
    [Header("Float")]
    [SerializeField] private float _characterHeight = 1f;
    [SerializeField] private float _springStrenght;
    [SerializeField] private float _springDampener;
    [SerializeField] private LayerMask _groundLayer;
    
    [Space(10)]
    [Header("Movement")]
    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _acceleration = 200f;
    [SerializeField] private AnimationCurve _accelerationFactorFromDot;
    [SerializeField] private float _maxAccelForce = 100f;
    [SerializeField] private AnimationCurve _maxAccelerationForceFactorFromDot;
    [SerializeField] private Vector3 _forceScale;
    
    [Space(10)]
    [Header("Camera Settings")]
    [SerializeField] private float _sensitivity = 0.03f;
    
    private float _xRotation = 0f;
    private float _yRotation = 0f;
    
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _cameraTransform = _cameraHolder.GetChild(0).transform;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void FixedUpdate()
    {
        Float();
        Move();
        HandleCamera();
    }

    private Vector3 MoveDirection()
    {
        // Get the input
        Vector3 moveInput = _input.MoveInput;
        Vector3 cameraForward = _cameraHolder.forward;
        Vector3 cameraRight = _cameraHolder.right;

        // Calculate the movement direction
        return (cameraForward * moveInput.z + cameraRight * moveInput.x).normalized;
    }
    
    private RaycastHit RaycastGround(float additional = 0f)
    {
        RaycastHit hit;
        if (Physics.Raycast(_rigidbody.position, Vector3.down, out hit, _characterHeight + additional, _groundLayer))
        {
            return hit;
        }

        return hit;
    }
    
    
    private void Float()
    {
        RaycastHit hit = RaycastGround(_characterHeight * 2f);
        if (hit.point != Vector3.zero)
        {
            Vector3 vel = _rigidbody.linearVelocity;

            float relDirVel = Vector3.Dot(Vector3.down, vel);

            float relVel = relDirVel;

            float x = hit.distance - _characterHeight;

            float springForce = (x * _springStrenght) - (relVel * _springDampener);
            _rigidbody.AddForce(Vector3.down * springForce);
        }
    }
    
    private Vector3 _storedVelocity;

    private void Move()
    {
        Vector3 move = MoveDirection();

        // do dotproduct of input to current goal velocity to apply acceleration based on dot to direction (makes sharp turns better).
        Vector3 unitVel = _storedVelocity.normalized;

        float velDot = Vector3.Dot(move, unitVel);
        float accel = _acceleration * _accelerationFactorFromDot.Evaluate(velDot);
        Vector3 goalVel = move * _maxSpeed;

        // lerp goal velocity towards new calculated goal velocity.
        _storedVelocity = Vector3.MoveTowards(_storedVelocity, goalVel, accel * Time.fixedDeltaTime);

        // calculate needed acceleration to reach goal velocity in a single fixed update.
        Vector3 neededAccel = (_storedVelocity - _rigidbody.linearVelocity) / Time.fixedDeltaTime;

        // clamp the needed acceleration to max possible acceleration.
        float maxAccel = _maxAccelForce * _maxAccelerationForceFactorFromDot.Evaluate(velDot);
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);

        _rigidbody.AddForce(Vector3.Scale(neededAccel * _rigidbody.mass, _forceScale));
    }

    private void HandleCamera()
    {
        // Get the mouse input
        float mouseX = _input.CameraInput.x * _sensitivity;
        float mouseY = _input.CameraInput.y * _sensitivity;
        
        // Add the mouse input to the rotation
        _xRotation -= mouseY;
        _yRotation += mouseX;
        
        // Clamp the x rotation
        _xRotation = Mathf.Clamp(_xRotation, -80f, 80f);
        
        // Apply the rotation
        _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, _yRotation, 0f);
    }
    
    public Transform[] GetBodyParts()
    {
        return new Transform[]
        {
            _head,
            _leftHand,
            _rightHand,
            _leftLeg,
            _rightLeg,
            _body
        };
    }
}
