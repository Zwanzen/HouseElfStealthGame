using System;
using UnityEngine;

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
    [SerializeField]
    private SpringJointSettings _springJointSettings;

    [SerializeField] private LayerMask _grabLayers;
    
    private PlayerController _player;
    private Transform _cameraTransform;
    
    private SpringJoint _springJoint;
    private bool _isGrabbing;
    private bool _isDoorGrabbing;
    private DoorController _doorController;
    
    private Vector3 _hitPoint;
    private float _hitDistance;
    
    
    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        InputManager.Instance.OnInteract += OnTryGrab;
    }

    private void Update()
    {
        if (_isGrabbing)
        {
            _springJoint.connectedAnchor = _cameraTransform.position + _cameraTransform.forward * _hitDistance;
        }
    }

    private void OnTryGrab()
    {
        if (_isGrabbing)
        {
            OnReleaseGrab();
            return;
        }
        
        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, 1f,
                _grabLayers))
        {
            _hitPoint = hit.point;
            if (hit.collider.tag == "Door")
            {
                _hitDistance = hit.distance;
                _isDoorGrabbing = true;
                _isGrabbing = true;
                _doorController = hit.transform.parent.GetComponent<DoorController>();
                _doorController.OnGrabDoor();
                ConstructSpringJoint(_doorController.Rigidbody);
                _springJoint.anchor = hit.point - _doorController.Rigidbody.position;
                return;
            }
            
            /*
            if (hit.rigidbody)
            {
                ConstructSpringJoint(hit.rigidbody);
                
                _isGrabbing = true;
            }
            */
        }
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
    }

    private void OnDrawGizmos()
    {

    }
}
