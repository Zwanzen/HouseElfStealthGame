using RootMotion.FinalIK;
using UnityEngine;

public class ProceduralSneakContext
{

    // Private variables
    [Header("Components")]
    private PlayerController _player;
    private ProceduralSneakStateMachine _stateMachine;
    private FullBodyBipedIK _bodyIK;
    
    [Header("Common")]
    private LayerMask _groundLayers;
    private Transform _leftFootTarget;
    private Transform _rightFootTarget;
    private Transform _leftFootRestTarget;
    private Transform _rightFootRestTarget;
    
    // Constructor
    public ProceduralSneakContext(PlayerController player, ProceduralSneakStateMachine stateMachine, FullBodyBipedIK bodyIK,
        LayerMask groundLayers, Transform leftFootTarget, Transform rightFootTarget, Transform leftFootRestTarget, Transform rightFootRestTarget)
    {
        _player = player;
        _stateMachine = stateMachine;
        _bodyIK = bodyIK;
        _groundLayers = groundLayers;
        _leftFootTarget = leftFootTarget;
        _rightFootTarget = rightFootTarget;
        _leftFootRestTarget = leftFootRestTarget;
        _rightFootRestTarget = rightFootRestTarget;
    }
    
    // Read only properties
    public PlayerController Player => _player;
    public FullBodyBipedIK BodyIK => _bodyIK;
    public Transform LeftFoot => _leftFootTarget;
    public Transform RightFoot => _rightFootTarget;
    
    // Public methods
    public RaycastHit GroundCast(Vector3 position, float distance)
    {
        Physics.Raycast(position, Vector3.down, out var hit, distance, _groundLayers);
        return hit;
    }
}