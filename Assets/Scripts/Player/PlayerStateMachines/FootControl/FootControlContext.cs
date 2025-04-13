using System;
using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("StateMachine")] [HideInInspector]
    public FootControlStateMachine SM;
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
    private MovementSettings _movementSettings;
    private MovementSettings _placementSettings;
    private AnimationCurve _speedCurve;
    private AnimationCurve _heightCurve;
    private AnimationCurve _placeCurve;
    private AnimationCurve _offsetCurve;
    
    public FootControlContext(PlayerController player, FullBodyBipedIK bodyIK, PlayerFootSoundPlayer footSoundPlayer,
        LayerMask groundLayers, Foot foot, Foot otherFoot, float stepLength, float stepHeight, MovementSettings movementSettings,
        MovementSettings placementSettings, AnimationCurve speedCurve, AnimationCurve heightCurve, AnimationCurve placeCurve, AnimationCurve offsetCurve)
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
        _placementSettings = placementSettings;
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
    public float StepHeight => _stepLength;
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
    public MovementSettings MovementSettings => _movementSettings;
    public MovementSettings PlacementSettings => _placementSettings;
    
    public Vector3 LastSafePosition { get; private set; }
    public Vector3 OldSafePosition { get; private set; }
    
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
        size *= 0.98f;
        
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
        var otherPlanted = OtherFoot.SM.State == FootControlStateMachine.EFootState.Planted;
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
        
        var defaultValues = FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        position.y += size.y;
        
        // We use a sphere cast to check with a radius downwards
        var upOffset = Vector3.up * _foot.Collider.size.y;
        if(!Physics.BoxCast(position, size, Vector3.down, out hit, rotation, maxDistance + size.y, GroundLayers))
            return false;
        
        return true;
    }
    
    public bool FootGroundCast(float minDist = 0f)
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
            result = footPos; // This will make the max distance min dist
        
        // This is the length between the foot and the max step length/intersection point
        var maxDistance = (result - footPos).magnitude;
        maxDistance = Mathf.Clamp(maxDistance, minDist, maxDistance);
        
        var defaultValues = FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        position.y += size.y;
        
        // We use a sphere cast to check with a radius downwards
        var upOffset = Vector3.up * _foot.Collider.size.y;
        if(!Physics.BoxCast(position, size, Vector3.down, rotation, maxDistance + size.y, GroundLayers))
            return false;
        
        return true;
    }
    
    public void SetSafePosition(Vector3 position)
    {
        // If the distance is great enough, we set the old safe position as well
        var dist = Vector3.Distance(position, OldSafePosition);
        if (dist is > 0.55f or < 0.1f)
            OldSafePosition = LastSafePosition;
        
        // Set the last safe position to the current one
        LastSafePosition = position;
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
        MoveRigidbody(Foot.Target, direction, _movementSettings);
    }
    
    public bool CheckStuckOnLedge(out RaycastHit hit)
    {
        // Do a box cast to check if the foot is stuck on a ledge
        var position = FootCastValues.Position;
        var size = FootCastValues.Size;
        var rotation = FootCastValues.Rotation;
        
        position.y += size.y;
        var scale = 1.04f;
        size = new Vector3(size.x * scale, size.y, size.z * scale);

        return Physics.BoxCast(position, size, Vector3.down, out hit, rotation, size.y * 1.5f,
            GroundLayers);
    }
}