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
    
    [Header("Control Variables")]
    private readonly float _springStrength;
    private readonly float _springDampener;
    
    // Constructor
    public PlayerControlContext(PlayerController player, PlayerControlStateMachine stateMachine, Rigidbody rigidbody, LayerMask groundLayers, float springStrength, float springDampener)
    {
        _player = player;
        _stateMachine = stateMachine;
        _rigidbody = rigidbody;
        _groundLayers = groundLayers;
        _springStrength = springStrength;
        _springDampener = springDampener;
    }
    
    // Read-only properties
    public PlayerController Player => _player;
    
    // Public methods
    public bool IsGrounded()
    {
        // Check if the player is grounded
        return Physics.Raycast(Player.Position, Vector3.down, PlayerController.Height + PlayerController.HeightThreshold, _groundLayers);
    }

    // Credit: https://youtu.be/qdskE8PJy6Q?si=hSfY9B58DNkoP-Yl
    public void RigidbodyFloat()
    {
        const float multiplier = 2f;
        if (!Physics.Raycast(Player.Position, Vector3.down, out var hit, PlayerController.Height * multiplier, _groundLayers)) return;
        var vel = _rigidbody.linearVelocity;

        var otherVel = Vector3.zero;
        var hitBody = hit.rigidbody;
        if (hitBody)
        {
            otherVel = hitBody.linearVelocity;
        }

        var relDirVel = Vector3.Dot(Vector3.down, vel);
        var otherDirVel = Vector3.Dot(Vector3.down, otherVel);

        var relVel = relDirVel - otherDirVel;

        var x = hit.distance - (PlayerController.Height + 0.02f);

        var springForce = (x * _springStrength) - (relVel * _springDampener);
        _rigidbody.AddForce(Vector3.down * springForce);

        // Add force to the hit rigidbody
        if (hitBody)
        {
            hitBody.AddForceAtPosition(Vector3.up * springForce, hit.point);
        }
    }
    
}
