using System;
using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using static CircleLineIntersection;
using static RigidbodyMovement;

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

    public FootControlContext(PlayerController player, FullBodyBipedIK bodyIK, PlayerFootSoundPlayer footSoundPlayer,
        LayerMask groundLayers, Foot foot, Foot otherFoot, float stepLength, float stepHeight, MovementSettings sneakMovementSettings, MovementSettings walkMovementSettings,
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
        SneakMovementSettings = sneakMovementSettings;
        WalkMovementSettings = walkMovementSettings;
        PlacementSettings = placementSettings;
        SpeedCurve = speedCurve;
        HeightCurve = heightCurve;
        PlaceCurve = placeCurve;
        OffsetCurve = offsetCurve;
        
        FootIKEffector = GetEffector();
        FootMapping = GetMapping();
        RotationOffset = GetRotationOffset();
    }
    
    // Read only properties
    public PlayerController Player => _player;
    public BoxCastValues FootCastValues => GetBoxCastValues();
    public IKEffector FootIKEffector { get; }
    public IKMappingLimb FootMapping { get; }

    public FullBodyBipedIK BodyIK => _bodyIK;
    public PlayerFootSoundPlayer FootSoundPlayer => _footSoundPlayer;
    public Foot Foot => _foot;
    public Foot OtherFoot => _otherFoot;
    public Quaternion RotationOffset { get; }
    public float StepLength => _stepLength;
    public float StepHeight => _stepLength;
    public LayerMask GroundLayers => GetGroundLayers();
    public Vector3 FootPlaceOffset =>  Vector3.up * 0.05f;
    public float FootRadius => 0.05f;
    public bool IsFootGrounded => GetFootGrounded();
    public bool IsFootLifting => GetIsLifting();
    public bool IsMBPressed => Foot.Side == Foot.EFootSide.Left ? InputManager.Instance.IsHoldingLMB : InputManager.Instance.IsHoldingRMB;
    public AnimationCurve SpeedCurve { get; }

    public AnimationCurve HeightCurve { get; }

    public AnimationCurve PlaceCurve { get; }

    public AnimationCurve OffsetCurve { get; }

    public MovementSettings SneakMovementSettings { get; }
    public MovementSettings WalkMovementSettings { get; }


    public MovementSettings PlacementSettings { get; }

    public Vector3 LastSafePosition { get; private set; }
    public Vector3 OldSafePosition { get; private set; }
    
    // Private methods
    private IKEffector GetEffector()
    {
        return _foot.Side == Foot.EFootSide.Left ? _bodyIK.solver.leftFootEffector : _bodyIK.solver.rightFootEffector;
    }

    private IKMappingLimb GetMapping()
    {
        return _foot.Side == Foot.EFootSide.Left ? BodyIK.solver.leftLegMapping : BodyIK.solver.rightLegMapping;
    }

    private Quaternion GetRotationOffset()
    {
        // If we are left foot, we need to rotate the foot 90 degrees to the left
        // If we are right foot, we need to rotate the foot 90 degrees to the right
        var rotation = Quaternion.Euler(0f, 0f, 0f);
        if (_foot.Side == Foot.EFootSide.Left)
            rotation *= Quaternion.Euler(0f, -90f, 0f);
        else
            rotation *= Quaternion.Euler(0f, 90f, 0f);
        return rotation;
    }

    // Depending on if we look down or not, we should include the props layer
    private LayerMask GetGroundLayers()
    {
        var layers = _groundLayers;
        // If we are not lifting our foot, we should include the props layer
        if (_foot.State != FootControlStateMachine.EFootState.Lifted)
            return layers;
             
        // If we are not looking down, remove the props layer
        if (_player.Camera.LookDownLerpOffset(10f) < 0f)
            layers &= ~(1 << LayerMask.NameToLayer("Props"));

        return layers;
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
        var otherNotLifted = OtherFoot.State != FootControlStateMachine.EFootState.Lifted;
        var input = _player.RelativeMoveInput.normalized;
        var closer = RelativeDistanceInDirection(_otherFoot.Target.position, _foot.Target.position, input) < 0f;
        
        if(LMB && Foot.Side == Foot.EFootSide.Left && otherNotLifted)
            return true;
        if(RMB && Foot.Side == Foot.EFootSide.Right && otherNotLifted)
            return true;
        if(_player.IsMoving && !_player.IsSneaking && otherNotLifted && closer)
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
    
    public bool FootGroundCast(float up, out RaycastHit hit)
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
        var upOffset = Vector3.up * (_foot.Collider.size.y + up);
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
        MoveRigidbody(Foot.Target, direction, SneakMovementSettings);
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