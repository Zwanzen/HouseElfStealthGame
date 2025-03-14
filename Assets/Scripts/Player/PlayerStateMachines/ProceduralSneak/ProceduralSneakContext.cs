using RootMotion.FinalIK;
using UnityEngine;
using static RigidbodyMovement;

public class ProceduralSneakContext
{

    // Private variables
    [Header("Components")]
    private PlayerController _player;
    private ProceduralSneakStateMachine _stateMachine;
    private FullBodyBipedIK _bodyIK;
    
    [Header("Common")]
    private LayerMask _groundLayers;
    private Rigidbody _leftFootTarget;
    private Rigidbody _rightFootTarget;
    private Transform _leftFootRestTarget;
    private Transform _rightFootRestTarget;
    
    [Header("Sneak Variables")]
    private float _sneakSpeed;
    private float _sneakStepLength;
    private MovementSettings _sneakMovementSettings;
    
    // Constructor
    public ProceduralSneakContext(PlayerController player, ProceduralSneakStateMachine stateMachine, FullBodyBipedIK bodyIK,
        LayerMask groundLayers, Rigidbody leftFootTarget, Rigidbody rightFootTarget, Transform leftFootRestTarget, Transform rightFootRestTarget,
        float sneakSpeed, float sneakStepLength, MovementSettings sneakMovementSettings)
    {
        _player = player;
        _stateMachine = stateMachine;
        _bodyIK = bodyIK;
        _groundLayers = groundLayers;
        _leftFootTarget = leftFootTarget;
        _rightFootTarget = rightFootTarget;
        _leftFootRestTarget = leftFootRestTarget;
        _rightFootRestTarget = rightFootRestTarget;
        _sneakSpeed = sneakSpeed;
        _sneakStepLength = sneakStepLength;
        _sneakMovementSettings = sneakMovementSettings;
    }
    
    // Read only properties
    public PlayerController Player => _player;
    public FullBodyBipedIK BodyIK => _bodyIK;
    public Rigidbody LeftFoot => _leftFootTarget;
    public Rigidbody RightFoot => _rightFootTarget;
    public float SneakSpeed => _sneakSpeed;
    public float SneakStepLength => _sneakStepLength;
    
    public Rigidbody LiftedFoot { get; set; }
    
    public Rigidbody PlantedFoot => GetOtherFoot(LiftedFoot);
    
    // Private methods
    private Rigidbody GetOtherFoot(Rigidbody foot)
    {
        return foot == _leftFootTarget ? _rightFootTarget : _leftFootTarget;
    }
    
    // Public methods
    public RaycastHit GroundCast(Vector3 position, float distance)
    {
        Physics.Raycast(position, Vector3.down, out var hit, distance, _groundLayers);
        return hit;
    }
    
    public Vector3 GetFeetMiddlePoint()
    {
        return Vector3.Lerp(_leftFootTarget.position, _rightFootTarget.position, 0.5f);
    }

    public Vector3 GetFootGroundPosition(Rigidbody foot)
    {
        var footPlaceOffset = Vector3.up * 0.05f;
        var groundCastUpOffset = Vector3.up * 0.1f;
        return GroundCast(foot.position + groundCastUpOffset, 1f).point + footPlaceOffset;
    }

    private Vector3 _storedBodyVel;
    public void MoveBody(Vector3 pos)
    {
        // Get body's current position
        var currentPos = _player.Rigidbody.position;
        currentPos.y = 0;
        pos.y = 0;
        
        // Get the direction to move
        var moveDir = (pos - currentPos);
        
        _storedBodyVel = MoveRigidbody(_player.Rigidbody, moveDir, _storedBodyVel, _sneakMovementSettings);
    }

    public void ResetBodyVel()
    {
        _storedBodyVel = Vector3.zero;
    }
}