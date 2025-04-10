using System;
using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;
using static CircleLineIntersection;
using static RigidbodyMovement;

[Serializable]
public struct Foot
{
    public enum EFootSide
    {
        Left,
        Right
    }

    private bool _isInStepRange;
    
    public Rigidbody Target;
    public Transform RestTarget;
    public BoxCollider Collider;
    public EFootSide Side;
    [HideInInspector]
    public FootControlStateMachine StateMachine;
}

public class FootControlContext
{
        
    [Header("Components")]
    private readonly PlayerController _player;
    private readonly FullBodyBipedIK _bodyIK;
    private PlayerFootSoundPlayer _footSoundPlayer;
    
    [Header("Common")]
    private LayerMask _groundLayers;
    private readonly Foot _foot;
    private readonly Foot _otherFoot;
    
    [Header("Foot Control Variables")]
    private readonly float _stepLength;
    private readonly float _stepHeight;
    private RigidbodyMovement.MovementSettings _movementSettings;
    private AnimationCurve _speedCurve;
    private AnimationCurve _heightCurve;
    private AnimationCurve _placeCurve;
    private AnimationCurve _offsetCurve;
    
    public FootControlContext(PlayerController player, FullBodyBipedIK bodyIK, PlayerFootSoundPlayer footSoundPlayer,
        LayerMask groundLayers, Foot foot, Foot otherFoot, float stepLength, float stepHeight, RigidbodyMovement.MovementSettings movementSettings,
        AnimationCurve speedCurve, AnimationCurve heightCurve, AnimationCurve placeCurve, AnimationCurve offsetCurve)
    {
        _player = player;
        _bodyIK = bodyIK;
        _footSoundPlayer = footSoundPlayer;
        _groundLayers = groundLayers;
        _foot = foot;
        _otherFoot = otherFoot;
        _stepLength = stepLength;
        _stepHeight = stepHeight;
        _movementSettings = movementSettings;
        _speedCurve = speedCurve;
        _heightCurve = heightCurve;
        _placeCurve = placeCurve;
        _offsetCurve = offsetCurve;
    }
    
    // Read only properties
    public PlayerController Player => _player;
    public BoxCastValues FootCastValues => GetBoxCastValues();
    public IKEffector FootIKEffector => GetEffector();
    public PlayerFootSoundPlayer FootSoundPlayer => _footSoundPlayer;
    public Foot Foot => _foot;
    public Foot OtherFoot => _otherFoot;
    public float StepLength => _stepLength;
    public float StepHeight => _stepHeight;
    public LayerMask GroundLayers => _groundLayers;
    public Vector3 FootPlaceOffset =>  Vector3.up * 0.05f;
    public float FootRadius => 0.05f;
    public bool IsFootGrounded => GetFootGrounded();
    public bool IsFootLifting => GetIsLifting();
    public AnimationCurve SpeedCurve => _speedCurve;
    public AnimationCurve HeightCurve => _heightCurve;
    public AnimationCurve PlaceCurve => _placeCurve;
    public AnimationCurve OffsetCurve => _offsetCurve;
    public bool BothInputsPressed => InputManager.Instance.IsHoldingLMB && InputManager.Instance.IsHoldingRMB;
    
    // Private methods
    private IKEffector GetEffector()
    {
        return _foot.Side == Foot.EFootSide.Left ? _bodyIK.solver.leftFootEffector : _bodyIK.solver.rightFootEffector;
    }
    
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

    private BoxCastValues GetBoxCastValues()
    {
        // Get the global position of the foot collider
        var position = _foot.Collider.transform.TransformPoint(_foot.Collider.center);
        
        // Get the size of the box collider 
        var size = _foot.Collider.size / 2f;
        // Reduce the size slightly
        size *= 0.9f;
        
        // Get the y-axis rotation of the foot
        var rotation = _foot.Target.rotation;
        rotation.x = 0f;
        rotation.z = 0f;
        rotation.Normalize();
        
        return new BoxCastValues(position, size, rotation);
    }
    
    private bool GetFootGrounded()
    {
        var downOffset = Vector3.down * FootCastValues.Size.y;
        return Physics.CheckBox(FootCastValues.Position + downOffset, FootCastValues.Size, FootCastValues.Rotation,GroundLayers);
    }
    
    private bool GetIsLifting()
    {
        var LMB = InputManager.Instance.IsHoldingLMB;
        var RMB = InputManager.Instance.IsHoldingRMB;
        var otherPlanted = OtherFoot.StateMachine.State == FootControlStateMachine.EFootState.Planted;
        if(LMB && Foot.Side == Foot.EFootSide.Left && otherPlanted)
            return true;
        if(RMB && Foot.Side == Foot.EFootSide.Right && otherPlanted)
            return true;
        return false;
    }
    
    // Public methods
    
    /// <summary>
    /// Checks if the foot has ground under it,
    /// But is also limited by the step length is a sphere radius.
    /// </summary>
    public bool FootGroundCast(out RaycastHit hit)
    {
        // We need to move the start pos of the ray backwards relative to the line direction
        var lineDir = Vector3.down;
        var footPos = Foot.Target.position;
        var otherFootPos = OtherFoot.Target.position;
        
        // Now we set the line start backwards
        // If the foot passed the radius, we still get the intersection point
        var offsetLineStart = footPos - lineDir;
        
        // We need to dynamically calculate the distance to the ground
        // based on the feet positions to not overshoot the step height
        // We use a custom-made CircleLineIntersection to calculate the distance
        if (!CalculateIntersectionPoint(otherFootPos, StepLength, offsetLineStart, lineDir,
                out var result))
            result = footPos; // This will make the max distance 0
        
        // This is the length between the foot and the max step length/intersection point
        var maxDistance = (result - footPos).magnitude;
        
        // Because sphere cast does not account for already overlapping colliders,
        // we check if the foot is already on the ground using overlap sphere
        // Implement this if we need it ^
        
        // We use a sphere cast to check with a radius downwards
        var upOffset = Vector3.up * _foot.Collider.size.y;
        if(!Physics.BoxCast(FootCastValues.Position + upOffset, FootCastValues.Size, Vector3.down, out hit, FootCastValues.Rotation, maxDistance + upOffset.magnitude, GroundLayers))
            return false;
        
        return true;
    }
    
    public float RelativeDistanceInDirection(Vector3 from, Vector3 to, Vector3 direction)
    {
        var fromTo = to - from;
        var dot = Vector3.Dot(direction.normalized, fromTo.normalized);
        return fromTo.magnitude * dot;
    }


    private float _lastMoveTime;
    private float _lerp;
    public void MoveFootToPosition(Vector3 direction)
    {
        // Move the foot to position using its rigidbody
        if(direction.magnitude > 1)
            direction.Normalize();
        /*

        // Calculate time since last call
        float currentTime = Time.time;
        float deltaTime = currentTime - _lastMoveTime;

        // If the direction is zero, we decrease the lerp value
        if(direction.magnitude < 0.1f)
            deltaTime = -deltaTime;

        // If it's been a while since last call, save negative time instead
        var timeToDecay = 0.2f;
        if (deltaTime > timeToDecay)
            deltaTime = -deltaTime;

        _lastMoveTime = currentTime;

        // Update lerp value over time
        float lerpSpeed = 2f; // Adjust this value to control acceleration rate
        _lerp = Mathf.Clamp01(_lerp + deltaTime * lerpSpeed);

        // Lerp between 0 and max speed
        var newSpeed = Mathf.Lerp(0f, _movementSettings.MaxSpeed, _lerp);

        var newMovementSettings = _movementSettings;
        newMovementSettings.MaxSpeed = newSpeed;
        */
        MoveRigidbody(Foot.Target, direction, _movementSettings);
    }
}