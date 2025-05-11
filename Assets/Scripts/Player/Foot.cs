using System;
using UnityEngine;

/// <summary>
/// This Struct is used to easily pass the foot refrence to the foot class.
/// </summary>
[Serializable]
public struct FootRef
{
    public FMODUnity.StudioEventEmitter SoundEmitter;

    [Header("Leg")]
    public Foot.EFootSide Side;
    public CapsuleCollider Thigh;
    public CapsuleCollider Calf;
    
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
        SoundEmitter = footRef.SoundEmitter;
    }
    
    // Properties
    public FMODUnity.StudioEventEmitter SoundEmitter { get; }
    public Vector3 Position => Target.position;
    public Quaternion Rotation => Target.rotation;
    public Vector3 XZForward => GetXZForward();
    public Vector3 Velocity => Target.linearVelocity;
    public Rigidbody Target { get; }
    public Transform Transform => Target.transform;
    public Vector3 RestPosition => _restTransform.position;
    public Vector3 RestDirection => _restTransform.forward;
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
    
    public CapsuleCollider Thigh { get; }
    public CapsuleCollider Calf { get; }
    public BoxCastValues FootCastValues => GetBoxCastValues();
    public float Momentum { get; private set; } = 0f;

    // Used to get values for box cast
    public struct BoxCastValues
    {
        public readonly Vector3 Position;
        public readonly Vector3 Size;
        public readonly Quaternion Rotation;

        public BoxCastValues(Vector3 position, Vector3 size, Quaternion rotation)
        {
            Position = position;
            Size = size;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Get the forward direction of the foot, based on world up.
    /// </summary>
    private Vector3 GetXZForward()
    {
        var dir = Target.transform.forward;
        dir.y = 0f;
        dir.Normalize();
        return dir;
    }

    private BoxCastValues GetBoxCastValues()
    {
        // Get the global position of the foot collider
        var position = Collider.transform.TransformPoint(Collider.center);

        // Get the size of the box collider 
        var size = Collider.size / 2f;
        // Reduce the size slightly
        size *= 0.98f;

        // Get the y-axis rotation of the foot
        var rotation = Target.rotation;
        rotation.x = 0f;
        rotation.z = 0f;
        rotation.Normalize();

        return new BoxCastValues(position, size, rotation);
    }

    // Public methods
    public void TurnOffCollider()
    {
        Collider.enabled = false;
        Thigh.enabled = false;
        Calf.enabled = false;
    }

    public void TurnOnCollider()
    {
        Collider.enabled = true;
        Thigh.enabled = true;
        Calf.enabled = true;
    }

    public void StopFoot()
    {
        _sm.TransitionToState(FootControlStateMachine.EFootState.Stop);
    }
    public void StartFoot()
    {
        _sm.TransitionToState(FootControlStateMachine.EFootState.Start);
    }

    /// <summary>
    /// Used when lifting foot?
    /// </summary>
    public void ResetMomentum()
    {
        Momentum = 0f;
    }

    /// <summary>
    /// Used to keep track of how fast the foot is moving.
    /// </summary>
    public void UpdateMomentum()
    {
        Momentum = Mathf.Lerp(Momentum, Velocity.magnitude, Time.deltaTime * 15f);
    }
}