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
    
    private PlayerFootSoundPlayer _leftFootSoundPlayer;
    private PlayerFootSoundPlayer _rightFootSoundPlayer;
    
    [Header("Sneak Variables")]
    private float _sneakSpeed;
    private float _sneakStepLength;
    private float _bodyRotationSpeed;
    private MovementSettings _liftedMovementSettings;
    private MovementSettings _plantedMovementSettings;
    private AnimationCurve _sneakSpeedCurve;
    
    // Constructor
    public ProceduralSneakContext(PlayerController player, ProceduralSneakStateMachine stateMachine, FullBodyBipedIK bodyIK,
        LayerMask groundLayers, Rigidbody leftFootTarget, Rigidbody rightFootTarget, Transform leftFootRestTarget, Transform rightFootRestTarget,
        float sneakSpeed, float sneakStepLength, float bodyRotationSpeed, MovementSettings liftedMovementSettings, MovementSettings plantedMovementSettings, AnimationCurve sneakSpeedCurve)
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
        _bodyRotationSpeed = bodyRotationSpeed;
        _liftedMovementSettings = liftedMovementSettings;
        _plantedMovementSettings = plantedMovementSettings;
        _sneakSpeedCurve = sneakSpeedCurve;
        
        // Initialize the foot sound players
        _leftFootSoundPlayer = _player.LeftFootSoundPlayer;
        _rightFootSoundPlayer = _player.RightFootSoundPlayer;
    }
    
    // Read only properties
    public PlayerController Player => _player;
    public FullBodyBipedIK BodyIK => _bodyIK;
    public Rigidbody LeftFoot => _leftFootTarget;
    public Rigidbody RightFoot => _rightFootTarget;
    public float SneakSpeed => _sneakSpeed;
    public float SneakStepLength => _sneakStepLength;
    public MovementSettings LiftedMovementSettings => _liftedMovementSettings;
    public MovementSettings PlantedMovementSettings => _plantedMovementSettings;
    public AnimationCurve SpeedCurve => _sneakSpeedCurve;
    
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
    
    private Vector3 _sLiftedFootGoalVel;
    public void MoveLiftedFoot(Vector3 direction)
    {
        _sLiftedFootGoalVel = MoveRigidbody(LiftedFoot, direction, _sLiftedFootGoalVel, _plantedMovementSettings);
    }
    
    public void ResetLiftedFootGoalVel()
    {
        _sLiftedFootGoalVel = LiftedFoot.linearVelocity;
    }
    
    private Vector3 _sBodyGoalVel;
    public void MoveBody(Vector3 pos)
    {
        // Get body's current position
        var currentPos = _player.Rigidbody.position;
        currentPos.y = 0;
        pos.y = 0;
        
        // Get the direction to move
        var moveDir = (pos - currentPos);
        
        _sBodyGoalVel = MoveRigidbody(_player.Rigidbody, moveDir, _sBodyGoalVel, _player.BodyMovementSettings);
    }

    // I think we should set the stored goal velocity to the current velocity of the body
    // when we start moving the body
    public void ResetBodyGoalVel()
    {
        _sBodyGoalVel = _player.Rigidbody.linearVelocity;
    }

    private float _turnLerpTimer;
    
    public void UpdateBodyRotation(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            _turnLerpTimer = 0;
            return;
        }
        _turnLerpTimer += Time.deltaTime;
        var rotSpeed = Mathf.Lerp(0, _bodyRotationSpeed, _turnLerpTimer);
        
        // Rotate the body towards the direction
        RotateRigidbody(_player.Rigidbody, direction, rotSpeed);
    }
    
    public bool FeetIsGrounded()
    {
        // Dist threshold to check if the feet are grounded
        const float distThreshold = 0.05f;
        
        // Get grounded positions
        var leftFootGroundPos = GetFootGroundPosition(_leftFootTarget);
        var rightFootGroundPos = GetFootGroundPosition(_rightFootTarget);
        
        // Check if the feet are grounded
        var leftFootDist = Vector3.Distance(_leftFootTarget.position, leftFootGroundPos);
        var rightFootDist = Vector3.Distance(_rightFootTarget.position, rightFootGroundPos);
        
        // Check if the feet are grounded
        if (leftFootDist < distThreshold && rightFootDist < distThreshold)
        {
            return true;
        }
        return false;
    }

    public void PlaySound(Rigidbody foot)
    {
        // Check which foot is the reference foot
        if (foot == _leftFootTarget)
        {
            _leftFootSoundPlayer.PlayFootSound(PlayerFootSoundPlayer.EFootSoundType.Wood);
        }
        else
        {
            _rightFootSoundPlayer.PlayFootSound(PlayerFootSoundPlayer.EFootSoundType.Wood);
        }
    }
}