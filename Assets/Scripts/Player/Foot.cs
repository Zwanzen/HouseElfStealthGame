using System;
using UnityEngine;

/// <summary>
/// This Struct is used to easily pass the foot refrence to the foot class.
/// </summary>
[Serializable]
public struct FootRef
{
    [Header("Leg")]
    public Foot.EFootSide Side;
    public Rigidbody Thigh;
    public Rigidbody Calf;
    public Transform ThighBone;
    public Transform CalfBone;
    
    [Header("Foot")]
    public Rigidbody Target;
    public Transform RestTarget;
    public Transform FootBone;
    public BoxCollider Collider;
}

/// <summary>
/// This class holds information about the foot, and is constructed by the player controller.
/// </summary>
public class Foot
{
    public enum EFootSide
    {
        Left,
        Right
    }

    // Foot References
    private readonly Transform _restTransform;
    private readonly Transform _footBone;
    private readonly FootControlStateMachine _sm;
    
    // Leg References
    private readonly Rigidbody _thigh;
    private readonly Transform _thighBone;
    private readonly Transform _calfBone;

    public Foot(FootRef footRef, FootControlStateMachine sm)
    {
        Target = footRef.Target;
        _restTransform = footRef.RestTarget;
        _footBone = footRef.FootBone;
        Collider = footRef.Collider;
        Side = footRef.Side;
        _sm = sm;
        Thigh = footRef.Thigh;
        Calf = footRef.Calf;
        ThighCollider = footRef.Thigh.GetComponentInChildren<CapsuleCollider>();
        CalfCollider = footRef.Calf.GetComponentInChildren<CapsuleCollider>();
        _thighBone = footRef.ThighBone;
        _calfBone = footRef.CalfBone;
    }
    
    // Properties
    public Vector3 Position => Target.position;
    public Rigidbody Target { get; }
    public Vector3 RestPosition => _restTransform.position;
    public Vector3 FootBonePosition => _footBone.position;
    public Quaternion FootBoneRotation => _footBone.rotation;
    public BoxCollider Collider { get; }
    public EFootSide Side { get; }
    // Common, so make it easy to access
    public bool Lifted => _sm.State == FootControlStateMachine.EFootState.Lifted;
    public bool Planted => _sm.State == FootControlStateMachine.EFootState.Planted;
    public bool Falling => _sm.State == FootControlStateMachine.EFootState.Falling;
    public bool Placing => _sm.State == FootControlStateMachine.EFootState.Placing;
    public FootControlStateMachine.EFootState State => _sm.State;
    public FootControlStateMachine Sm => _sm;
    
    public Rigidbody Thigh { get; }
    public Rigidbody Calf { get; }
    public CapsuleCollider ThighCollider { get; }
    public CapsuleCollider CalfCollider { get; }
    
    public Vector3 ThighPosition => _calfBone.position;
    public Quaternion ThighRotation => _thighBone.rotation;
    public Vector3 CalfPosition => _calfBone.position;
    public Quaternion CalfRotation => _calfBone.rotation;
}