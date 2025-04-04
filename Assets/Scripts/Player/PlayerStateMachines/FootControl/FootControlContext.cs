using System;
using RootMotion.FinalIK;
using UnityEngine;
using static CircleLineIntersection;

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
    public EFootSide Side;
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
    
    public FootControlContext(PlayerController player, FullBodyBipedIK bodyIK, PlayerFootSoundPlayer footSoundPlayer,
        LayerMask groundLayers, Foot foot, Foot otherFoot, float stepLength, float stepHeight, RigidbodyMovement.MovementSettings movementSettings)
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
    }
    
    // Read only properties
    public PlayerController Player => _player;
    public IKEffector FootIKEffector => GetEffector();
    public Foot Foot => _foot;
    public Foot OtherFoot => _otherFoot;
    public float StepLength => _stepLength;
    public float StepHeight => _stepHeight;
    public LayerMask GroundLayers => _groundLayers;
    public Vector3 FootPlaceOffset =>  Vector3.up * 0.05f;
    public float FootRadius => 0.05f;
    public bool IsFootGrounded => GetFootGrounded();
    public bool IsFootLifting => GetIsLifting();
    
    // Private methods
    private IKEffector GetEffector()
    {
        return _foot.Side == Foot.EFootSide.Left ? _bodyIK.solver.leftFootEffector : _bodyIK.solver.rightFootEffector;
    }
    
    private bool GetFootGrounded()
    {
        return Physics.CheckSphere(Foot.Target.position - FootPlaceOffset, FootRadius/2f, GroundLayers);
    }
    
    private bool GetIsLifting()
    {
        var LMB = InputManager.Instance.IsHoldingLMB;
        var RMB = InputManager.Instance.IsHoldingRMB;
        if(LMB && Foot.Side == Foot.EFootSide.Left)
            return true;
        if(RMB && Foot.Side == Foot.EFootSide.Right)
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
        // We need to dynamically calculate the distance to the ground
        // based on the feet positions to not overshoot the step height
        // We use a custom-made CircleLineIntersection to calculate the distance
        CalculateIntersectionPoint(OtherFoot.Target.position, StepLength, Foot.Target.position, Vector3.down,
            out var result);

        
        // This is the length between the foot and the max step length/intersection point
        var maxDistance = (result - Foot.Target.position).magnitude;
        
        Debug.DrawLine(Foot.Target.position, Foot.Target.position + Vector3.down * maxDistance, Color.red);
        
        // Because sphere cast does not account for already overlapping colliders,
        // we check if the foot is already on the ground using overlap sphere
        // Implement this if we need it ^
        
        // We use a sphere cast to check with a radius downwards
        var upOffset = Vector3.up * (FootRadius * 2f);
        if(!Physics.SphereCast(Foot.Target.position + upOffset, FootRadius, Vector3.down, out hit, maxDistance + upOffset.magnitude, GroundLayers))
            return false;
        
        return true;
    }
    
    public float RelativeDistanceInDirection(Vector3 from, Vector3 to, Vector3 direction)
    {
        var fromTo = to - from;
        var dot = Vector3.Dot(direction.normalized, fromTo.normalized);
        return fromTo.magnitude * dot;
    }
    
    private Vector3 _sGoalVelocity;
    public void MoveFootToPosition(Vector3 direction)
    {
        // Move the foot to position using its rigidbody
        if(direction.magnitude > 1)
            direction.Normalize();
        
        if (!CalculateIntersectionPoint(OtherFoot.Target.position, StepLength,
                Foot.Target.position, Vector3.down,
                out var result))
            direction = OtherFoot.Target.position - Foot.Target.position;

        _foot.Target.linearVelocity = direction * _movementSettings.Acceleration;
    }
    
    public void ResetGoalVelocity()
    {
        _sGoalVelocity = Vector3.zero;
    }
}