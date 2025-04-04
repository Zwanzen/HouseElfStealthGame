using System;
using UnityEngine;

public class TempMuscle : MonoBehaviour
{
    [SerializeField] private Transform _target;
    
    private Rigidbody _rigidbody;

    private Quaternion rotationRelativeToTarget;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        rotationRelativeToTarget = Quaternion.Inverse(_target.rotation) * transform.rotation;
    }

    private void FixedUpdate()
    {
        
        Vector3 p = _target.position;
        Quaternion r = _target.rotation * rotationRelativeToTarget;
        transform.SetPositionAndRotation(p, r);
        _rigidbody.MovePosition(p);
        _rigidbody.MoveRotation(r);
    }
}
