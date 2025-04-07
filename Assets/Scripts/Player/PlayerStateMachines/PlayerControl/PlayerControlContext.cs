using RootMotion.Dynamics;
using UnityEngine;
using static RigidbodyMovement;

public class PlayerControlContext
{

    // Private variables
    [Header("Components")]
    private readonly PlayerController _player;
    private PlayerControlStateMachine _stateMachine;
    private readonly Rigidbody _rigidbody;
    
    [Header("Common")]
    private readonly LayerMask _groundLayers;
    private readonly Foot _leftFoot;
    private readonly Foot _rightFoot;
    
    [Header("Control Variables")]
    private readonly float _springStrength;
    private readonly float _springDampener;
    private MovementSettings _bodyMovementSettings;
    
    // Constructor
    public PlayerControlContext(PlayerController player, PlayerControlStateMachine stateMachine, Rigidbody rigidbody, LayerMask groundLayers,
        Foot leftFoot, Foot rightFoot, float springStrength, float springDampener, MovementSettings bodyMovementSettings)
    {
        _player = player;
        _stateMachine = stateMachine;
        _rigidbody = rigidbody;
        _groundLayers = groundLayers;
        _leftFoot = leftFoot;
        _rightFoot = rightFoot;
        _springStrength = springStrength;
        _springDampener = springDampener;
        _bodyMovementSettings = bodyMovementSettings;
    }
    
    // Read-only properties
    public PlayerController Player => _player;
    public Foot LeftFoot => _leftFoot;
    public Foot RightFoot => _rightFoot;
    public LayerMask GroundLayers => _groundLayers;
    
    // Private methods
    private Vector3 GetLowestFootPosition()
    {
        // Get the lowest foot position
        Vector3 leftFootPos = _leftFoot.Target.position;
        Vector3 rightFootPos = _rightFoot.Target.position;
        return leftFootPos.y < rightFootPos.y ? leftFootPos : rightFootPos;
    }
    
    // Public methods
    public bool IsGrounded()
    {
        // Check if the feet are grounded using CheckSphere
        return Physics.CheckSphere(_leftFoot.Target.position, 0.8f, _groundLayers) ||
               Physics.CheckSphere(_rightFoot.Target.position, 0.8f, _groundLayers);
    }

    // Credit: https://youtu.be/qdskE8PJy6Q?si=hSfY9B58DNkoP-Yl
    public void RigidbodyFloat()
    {
        const float multiplier = 2f;
        if (!Physics.Raycast(Player.Position, Vector3.down, out var hit, PlayerController.Height * multiplier, _groundLayers)) return;
        var vel = _rigidbody.linearVelocity;

        var relDirVel = Vector3.Dot(Vector3.down, vel);

        var relVel = relDirVel;
        
        /*
        // Calculate the distance from player to lowest foot
        var footPos = new Vector3(Player.Position.x, GetLowestFootPosition().y, Player.Position.z);
        var footDistance = Vector3.Distance(Player.Position, footPos);
        */
        
        var x = hit.distance - (PlayerController.Height + 0.02f);

        var springForce = (x * _springStrength) - (relVel * _springDampener);
        _rigidbody.AddForce(Vector3.down * springForce);
    }
    
    public void MoveBody(Vector3 targetPosition)
    {
        var currentPos = Player.Position;
        currentPos.y = 0f;
        var dir = targetPosition - currentPos;
        if(dir.magnitude > 1f)
            dir.Normalize();
        MoveRigidbody(Player.Rigidbody, dir, _bodyMovementSettings);
    }

    public Vector3 BetweenFeet(float lerp)
    {
        var pos = Vector3.Lerp(_leftFoot.Target.position, _rightFoot.Target.position, lerp);
        pos.y = 0;
        return pos;
    }

    public float FeetLerp()
    {
        // Find out what foot is lifting
        var leftLift = _leftFoot.StateMachine.State == FootControlStateMachine.EFootState.Lifted;
        var rightLift = _rightFoot.StateMachine.State == FootControlStateMachine.EFootState.Lifted;

        if (leftLift)
            return 0.8f;
        
        if (rightLift)
            return 0.2f;

        return 0.5f;
    }
    

    
}
