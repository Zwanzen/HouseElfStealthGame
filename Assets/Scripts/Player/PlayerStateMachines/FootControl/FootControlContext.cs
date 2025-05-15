using System;
using FMOD.Studio;
using FMODUnity;
using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using static CircleLineIntersection;
using static RigidbodyMovement;
using static SoundGameplayManager;

public class FootControlContext
{
        
    [Header("Components")]
    private readonly PlayerController _player;
    private readonly FullBodyBipedIK _bodyIK;

    [Header("Common")]
    private LayerMask _groundLayers;
    private readonly Foot _foot;
    private readonly Foot _otherFoot;
    
    private readonly MovementSettings _minSneakSetting;
    private readonly MovementSettings _maxSneakSettings;

    private readonly MovementSettings _minWalkSetting;
    private readonly MovementSettings _maxWalkSettings;

    public FootControlContext(PlayerController player, FullBodyBipedIK bodyIK,
        LayerMask groundLayers, Foot foot, Foot otherFoot, float stepLength, float stepHeight,
        MovementSettings minSneakSetting, MovementSettings maxSneakSetting, MovementSettings minWalkSettings, MovementSettings maxWalkSettings,
        MovementSettings placementSettings, AnimationCurve speedCurve, AnimationCurve heightCurve, AnimationCurve placeCurve, AnimationCurve offsetCurve)
    {
        _player = player;
        _bodyIK = bodyIK;
        _groundLayers = groundLayers;
        _foot = foot;
        _otherFoot = otherFoot;
        StepLength = stepLength;
        StepHeight = stepHeight;

        _minSneakSetting = minSneakSetting;
        _maxSneakSettings = maxSneakSetting;
        _minWalkSetting = minWalkSettings;
        _maxWalkSettings = maxWalkSettings;

        PlaceSettings = placementSettings;
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
    public Foot.BoxCastValues FootCastValues => Foot.FootCastValues;
    public IKEffector FootIKEffector { get; }
    public IKMappingLimb FootMapping { get; }
    public FullBodyBipedIK BodyIK => _bodyIK;
    public Foot Foot => _foot;
    public Foot OtherFoot => _otherFoot;
    public Vector3 LowestFootPosition => _foot.Position.y < _otherFoot.Position.y ? _foot.Position : _otherFoot.Position;
    public Quaternion RotationOffset { get; }
    public float StepLength { get; }
    public float StepHeight { get; }
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

    /// <summary>
    /// The lerped sneak settings based on the current speed.
    /// </summary>
    public MovementSettings CurrentSneakSettings => _minSneakSetting.Lerp(_maxSneakSettings, _player.CurrentPlayerSpeed);

    /// <summary>
    /// The lerped walk settings based on the current speed.
    /// </summary>
    public MovementSettings CurrentWalkSettings => _minWalkSetting.Lerp(_maxWalkSettings, _player.CurrentPlayerSpeed);

    /// <summary>
    /// The settings used for placing the foot.
    /// </summary>
    public MovementSettings PlaceSettings { get; }
    
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
    
    private bool GetFootGrounded()
    {
        var downOffset = Vector3.down * FootCastValues.Size.y;
        return Physics.CheckBox(FootCastValues.Position + downOffset, FootCastValues.Size, FootCastValues.Rotation,GroundLayers);
    }

    private float _placeTimer;
    private bool GetIsLifting()
    {
        var LMB = InputManager.Instance.IsHoldingLMB;
        var RMB = InputManager.Instance.IsHoldingRMB;
        var otherNotLifted = OtherFoot.State != FootControlStateMachine.EFootState.Lifted;
        var input = _player.RelativeMoveInput.normalized;
        var closer = RelativeDistanceInDirection(_otherFoot.Target.position, _foot.Target.position, input) < 0f;

        // Temp leap
        var dist = Vector3.Distance(Foot.Position, OtherFoot.Position);
        var relDown = Foot.Position.y - OtherFoot.Position.y;
        var otherHasGround = FootGroundCast(OtherFoot, 0.23f);
        if (OtherFoot.Placing && !otherHasGround)
        {
            _placeTimer += Time.deltaTime;
            if (_placeTimer > 0.13f)
            {
                _placeTimer = 0f;
                return true;
            }
        }
        else
        {
            _placeTimer = 0f;
        }

        if (LMB && Foot.Side == Foot.EFootSide.Left && otherNotLifted)
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
    
    public bool FootGroundCast(float dist, out RaycastHit hit)
    {
        // We need to move the start pos of the ray backwards relative to the line direction
        var lineDir = Vector3.down;
        var footPos = Foot.Target.position;
        
        var defaultValues = FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        position.y += size.y;

        var upOffset = Vector3.up * _foot.Collider.size.y;
        if(!Physics.BoxCast(position, size, Vector3.down, out hit, rotation, dist + size.y, GroundLayers))
            return false;


        return true;
    }

    public bool FootGroundCast(Foot foot, float dist)
    {
        // We need to move the start pos of the ray backwards relative to the line direction
        var lineDir = Vector3.down;
        var footPos = foot.Target.position;

        var defaultValues = foot.FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        position.y += size.y;

        // We use a sphere cast to check with a radius downwards
        var upOffset = Vector3.up * _foot.Collider.size.y;
        if (!Physics.BoxCast(position, size, Vector3.down, rotation, dist, GroundLayers))
            return false;

        return true;
    }

    public float RelativeDistanceInDirection(Vector3 from, Vector3 to, Vector3 direction)
    {
        var fromTo = to - from;
        var dot = Vector3.Dot(direction.normalized, fromTo.normalized);
        return fromTo.magnitude * dot;
    }


    public void MoveFootToPosition(Vector3 direction)
    {
        // Move the foot to position using its rigidbody
        if(direction.magnitude > 1)
            direction.Normalize();
        MoveRigidbody(Foot.Target, direction, CurrentSneakSettings);
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

    public void PlayFootSound()
    {
        // Check colliders below the foot for tag
        var defaultValues = FootCastValues;
        var position = defaultValues.Position;
        var size = defaultValues.Size;
        var rotation = defaultValues.Rotation;
        position.y += size.y/2;
        var layers = GroundLayers;
        // Exlude props layer
        layers &= ~(1 << LayerMask.NameToLayer("Props"));
        if (!Physics.BoxCast(position, size, Vector3.down, out var hit, rotation, size.y * 2f, layers))
            return;
        var material = SoundGameplayManager.Instance.TryGetMaterialFromTag(hit.collider.tag);
        var mag = Mathf.Clamp(Foot.Momentum, 0.1f, Foot.Momentum);
        mag *= _player.CurrentPlayerSpeed;
        SoundGameplayManager.Instance.PlayPlayerStepAtPosition(Foot.SoundEmitter, material, Foot.Position, mag);
    }
}