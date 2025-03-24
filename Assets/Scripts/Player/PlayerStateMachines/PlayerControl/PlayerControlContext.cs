using UnityEngine;

public class PlayerControlContext
{

    // Private variables
    [Header("Components")]
    private readonly PlayerController _player;
    private PlayerControlStateMachine _stateMachine;
    private readonly Rigidbody _rigidbody;
    
    [Header("Common")]
    private readonly LayerMask _groundLayers;
    private readonly Rigidbody _leftFootTarget;
    private readonly Rigidbody _rightFootTarget;
    
    [Header("Control Variables")]
    private readonly float _springStrength;
    private readonly float _springDampener;
    
    // Constructor
    public PlayerControlContext(PlayerController player, PlayerControlStateMachine stateMachine, Rigidbody rigidbody, LayerMask groundLayers,
        Rigidbody leftFootTarget, Rigidbody rightFootTarget, float springStrength, float springDampener)
    {
        _player = player;
        _stateMachine = stateMachine;
        _rigidbody = rigidbody;
        _groundLayers = groundLayers;
        _leftFootTarget = leftFootTarget;
        _rightFootTarget = rightFootTarget;
        _springStrength = springStrength;
        _springDampener = springDampener;
    }
    
    // Read-only properties
    public PlayerController Player => _player;
    
    // Private methods
    private Vector3 GetLowestFootPosition()
    {
        // Get the lowest foot position
        Vector3 leftFootPos = _leftFootTarget.position;
        Vector3 rightFootPos = _rightFootTarget.position;
        return leftFootPos.y < rightFootPos.y ? leftFootPos : rightFootPos;
    }
    
    // Public methods
    public bool IsGrounded()
    {
        // Check if the feet are grounded using CheckSphere
        return Physics.CheckSphere(_leftFootTarget.position, PlayerController.HeightThreshold, _groundLayers) ||
               Physics.CheckSphere(_rightFootTarget.position, PlayerController.HeightThreshold, _groundLayers);
    }

    // Credit: https://youtu.be/qdskE8PJy6Q?si=hSfY9B58DNkoP-Yl
    public void RigidbodyFloat()
    {
        const float multiplier = 2f;
        //if (!Physics.Raycast(Player.Position, Vector3.down, out var hit, PlayerController.Height * multiplier, _groundLayers)) return;
        var vel = _rigidbody.linearVelocity;

        var relDirVel = Vector3.Dot(Vector3.down, vel);

        var relVel = relDirVel;
        
        // Calculate the distance from player to lowest foot
        var footPos = new Vector3(Player.Position.x, GetLowestFootPosition().y, Player.Position.z);
        var footDistance = Vector3.Distance(Player.Position, footPos);
        
        var x = footDistance - (PlayerController.Height + 0.02f);

        var springForce = (x * _springStrength) - (relVel * _springDampener);
        _rigidbody.AddForce(Vector3.down * springForce);
    }
    
}
