using System;
using RootMotion.FinalIK;
using UnityEngine;
using static FootControlStateMachine;
using static CircleLineIntersection;

[Serializable]
public struct Foot
{
    public enum EFootSide
    {
        Left,
        Right
    }
    
    private EFootState _footState;
    
    public Rigidbody Target;
    public Transform RestTarget;
    public EFootSide Side;
    public EFootState State => _footState;

    public void SetFootState(EFootState state)
    {
        _footState = state;
    }
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
    
    public FootControlContext(PlayerController player, FullBodyBipedIK bodyIK, PlayerFootSoundPlayer footSoundPlayer,
        LayerMask groundLayers, Foot foot, Foot otherFoot, float stepLength, float stepHeight)
    {
        _player = player;
        _bodyIK = bodyIK;
        _footSoundPlayer = footSoundPlayer;
        _groundLayers = groundLayers;
        _foot = foot;
        _otherFoot = otherFoot;
        _stepLength = stepLength;
        _stepHeight = stepHeight;
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
    public float FootRadius => 0.1f;
    public bool IsFootGrounded => GetFootGrounded();
    
    // Private methods
    private IKEffector GetEffector()
    {
        return _foot.Side == Foot.EFootSide.Left ? _bodyIK.solver.leftFootEffector : _bodyIK.solver.rightFootEffector;
    }
    
    private bool GetFootGrounded()
    {
        return Physics.CheckSphere(Foot.Target.position, FootRadius, GroundLayers);
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
        
        // We use a sphere cast to check with a radius
        if(!Physics.SphereCast(Foot.Target.position, FootRadius, Vector3.down, out hit, maxDistance, GroundLayers))
            return false;
        
        return true;
    }
    
    public float RelativeDistanceInDirection(Vector3 from, Vector3 to, Vector3 direction)
    {
        var fromTo = to - from;
        var dot = Vector3.Dot(direction.normalized, fromTo.normalized);
        return fromTo.magnitude * dot;
    }
    
    public void MoveFootToPosition(Vector3 position)
    {
        // Move the foot to position using its rigidbody
        var direction = position - Foot.Target.position;
        if(direction.magnitude > 1)
            direction.Normalize();
        Foot.Target.linearVelocity = direction;
    }
}