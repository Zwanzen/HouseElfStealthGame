using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct SpringJointSettings
{
    public float Spring;
    public float Damper;
    public float MinDistance;
    public float MaxDistance;
    public float Tolerance;

}

public class PlayerGrabController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _maxGrabDistance = 1f;
    [Space(10f)]
    [SerializeField]
    private SpringJointSettings _springJointSettings;
    [SerializeField] private LayerMask _grabLayers;
    private PlayerController _player;
    
    [FormerlySerializedAs("_MaxLineWidth")]
    [Space(10f)]
    [Header("Line Renderer")]
    [SerializeField] private float _startWidth = 0.1f;
    [SerializeField] private float _endWidth = 0.01f;
    [SerializeField] private float _lineRenderSpeed = 1.50f;
    [SerializeField] private float _lineRenderDuration = 0.35f;
    [SerializeField] private AnimationCurve _lineScaleCurve;

    private LineRenderer _lineRenderer;
    private float _lineRenderTimer;
    
    
    private Transform _cameraTransform;
    private SpringJoint _springJoint;
    private bool _isGrabbing;
    private bool _isDoorGrabbing;
    private DoorController _doorController;
    private float _currentGrabDistance;
    
    
    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
        _player = GetComponent<PlayerController>();
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.enabled = false;
    }
    
    private void UpdateGrabDistance(int scroll)
    {
        if(!_isGrabbing) return;
        float intToFloat = 0f;
        if (scroll == 1)
        {
            intToFloat = 0.1f;
        }else if (scroll == -1)
        {
            intToFloat = -0.1f;
        }
        _currentGrabDistance += intToFloat;
        _currentGrabDistance = Mathf.Clamp(_currentGrabDistance, 0.5f, _maxGrabDistance);
    }

    private void Start()
    {
        InputManager.Instance.OnInteract += OnTryGrab;
        InputManager.Instance.OnScroll += UpdateGrabDistance;
    }

    private void Update()
    {
        if (_isGrabbing)
        {
            _springJoint.connectedAnchor = _cameraTransform.position + _cameraTransform.forward * _currentGrabDistance;
            _lineRenderer.SetPosition(0, _cameraTransform.position + _cameraTransform.forward * _currentGrabDistance);
            _lineRenderer.SetPosition(1, _springJoint.transform.position);
        }
        else
        {
            // Move linerender index 0 towards index 1
            _lineRenderer.SetPosition(0, Vector3.Lerp(_lineRenderer.GetPosition(0), _lineRenderer.GetPosition(1), Time.deltaTime * 5f));
        }
        
        HandleLineRenderer();
    }
    
    private void HandleLineRenderer()
    {
        if (_isGrabbing)
        {
            _lineRenderTimer += Time.deltaTime * _lineRenderSpeed;
        }else
        {
            _lineRenderTimer -= Time.deltaTime * _lineRenderSpeed;
        }

        var lerp = _lineRenderTimer / _lineRenderDuration;
        _lineRenderer.startWidth = Mathf.Lerp(0f, _endWidth, _lineScaleCurve.Evaluate(lerp));
        _lineRenderer.endWidth = Mathf.Lerp(0f, _startWidth, _lineScaleCurve.Evaluate(lerp));

        _lineRenderTimer = Mathf.Clamp(_lineRenderTimer, 0f, 1f);
        if(_lineRenderTimer <= 0f)
        {
            _lineRenderer.enabled = false;
        }else
        {
            _lineRenderer.enabled = true;
        }
    }

    private void OnTryGrab()
    {
        if (_isGrabbing)
        {
            OnReleaseGrab();
            return;
        }

        if (!Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _maxGrabDistance,
                _grabLayers)) return;
        
        if (!hit.collider.CompareTag("Door")) return;
        
        _currentGrabDistance = hit.distance;
        _isDoorGrabbing = true;
        _isGrabbing = true;
        _doorController = hit.transform.parent.GetComponent<DoorController>();
        _doorController.OnGrabDoor();
        ConstructSpringJoint(_doorController.Rigidbody);
    }
    
    private void OnReleaseGrab()
    {
        if (_springJoint)
        {
            if (_isDoorGrabbing)
            {
                _isDoorGrabbing = false;
                _doorController.OnReleaseDoor();
            }
            
            _isGrabbing = false;
            Destroy(_springJoint);
        }
    }
    
    private void ConstructSpringJoint(Rigidbody target)
    {
        // Create a new spring joint
        _springJoint = target.gameObject.AddComponent<SpringJoint>();
        _springJoint.autoConfigureConnectedAnchor = false;
        
        // Set the spring joint settings
        _springJoint.spring = _springJointSettings.Spring;
        _springJoint.damper = _springJointSettings.Damper;
        _springJoint.minDistance = _springJointSettings.MinDistance;
        _springJoint.maxDistance = _springJointSettings.MaxDistance;
        _springJoint.anchor = Vector3.zero;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from input
        InputManager.Instance.OnInteract -= OnTryGrab;
        InputManager.Instance.OnInteract -= OnTryGrab;
    }
}
