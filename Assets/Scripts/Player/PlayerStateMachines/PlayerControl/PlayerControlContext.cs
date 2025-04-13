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
    private readonly float _lowestBodyHeight;
    private AnimationCurve _distanceHeightCurve;
    private MovementSettings _bodyMovementSettings;
    
    // Constructor
    public PlayerControlContext(PlayerController player, PlayerControlStateMachine stateMachine, Rigidbody rigidbody, LayerMask groundLayers,
        Foot leftFoot, Foot rightFoot, float springStrength, float springDampener, MovementSettings bodyMovementSettings, AnimationCurve distanceHeightCurve,
        float lowestBodyHeight)
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
        _distanceHeightCurve = distanceHeightCurve;
        _lowestBodyHeight = lowestBodyHeight;
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
    // Modified
    public void RigidbodyFloat()
    {
        var vel = _rigidbody.linearVelocity;

        var relDirVel = Vector3.Dot(Vector3.down, vel);

        var relVel = relDirVel;
        
        // Calculate the distance from player to lowest foot
        var lowestFootPos = new Vector3(Player.Position.x, GetLowestFootPosition().y, Player.Position.z);
        var distanceToLowestFoot = Vector3.Distance(Player.Position, lowestFootPos);
        
        // Offset from the foot to the ground
        var footPlaceOffset = 0.05f;
        
        // We want to change the height of the player based on the distance between the feet
        // We only want to change the distance on the xz plane, if the lifted foot is going upwards, we don't want our body to go down
        // But we do want the body to go down if the lifted foot is going downwards
        
        // Find out if we are lifting a foot
        var isLifting = _leftFoot.SM.State == FootControlStateMachine.EFootState.Lifted ||
                       _rightFoot.SM.State == FootControlStateMachine.EFootState.Lifted;
        
        // Get lifted foot position and the other foot position
        var liftedFootPos = _leftFoot.SM.State == FootControlStateMachine.EFootState.Lifted ? _leftFoot.Target.position : _rightFoot.Target.position;
        var otherFootPos = _leftFoot.SM.State == FootControlStateMachine.EFootState.Lifted ? _rightFoot.Target.position: _leftFoot.Target.position;

        // If the lifted foot is not below the other foot, we don't want height influence
        var shouldCare = liftedFootPos.y < otherFootPos.y + 0.10f && isLifting;
        if (!shouldCare)
            liftedFootPos.y = otherFootPos.y;
        
        var distBetweenFeet = Vector3.Distance(liftedFootPos, lowestFootPos);
        var lerp = distBetweenFeet / _player.StepLength;
        if (shouldCare)
            lerp = distBetweenFeet / 0.20f;

        var highest = PlayerController.Height;

        var height = Mathf.Lerp(highest, _lowestBodyHeight, _distanceHeightCurve.Evaluate(lerp));
        
        var x = distanceToLowestFoot - (height + footPlaceOffset);
        
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
        var leftLift = _leftFoot.SM.State == FootControlStateMachine.EFootState.Lifted;
        var rightLift = _rightFoot.SM.State == FootControlStateMachine.EFootState.Lifted;
        
        var running = InputManager.Instance.IsRunning;
        
        var dir = _player.Rigidbody.transform.forward;
        
        var dist = Vector3.Distance(_leftFoot.Target.position, _rightFoot.Target.position);
        var start = _player.StepLength * 0.7f;

        if (running)
        {
            // A number between -stepLength and stepLength
            var relDist = RelativeDistanceInDirection(_leftFoot.Target.position, _rightFoot.Target.position, dir);
            // Create a lerp between -stepLength and stepLength
            var lerp = relDist / _player.StepLength;
            // Make it between 0 and 1
            lerp = (lerp + 1) / 2;
            // Use the lerp to return a value
            Debug.Log(lerp);
            return Mathf.Lerp(0.2f,0.8f, lerp);
        }
        
        if (leftLift)
        {
            var lerp = dist - start;
            lerp /= _player.StepLength;
            return Mathf.Lerp(0.8f, 0.5f, lerp);
        }

        if (rightLift)
        {
            var lerp = dist - start;
            lerp /= _player.StepLength;
            return Mathf.Lerp(0.2f, 0.5f, lerp);
        }

        return 0.5f;
    }
    
    public float RelativeDistanceInDirection(Vector3 from, Vector3 to, Vector3 direction)
    {
        var fromTo = to - from;
        var dot = Vector3.Dot(direction.normalized, fromTo.normalized);
        return fromTo.magnitude * dot;
    }
    
    public void UpdateBodyRotation(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            return;
        }

        // Other direction
        var otherDir = _player.RelativeMoveInput;
        // Get the dot between camera and other direction
        var dot = Vector3.Dot(_player.Camera.GetCameraYawTransform().forward.normalized, otherDir.normalized);
        if (dot < -0.2f)
            otherDir = -otherDir;
        
        // Downwards lerp
        var angle = _player.Camera.CameraX;
        var dir = Vector3.Lerp(otherDir, direction, angle/60f);
        
        // Get the dot between the lerped direction and the player's forward direction
        var dot2 = Vector3.Dot(dir.normalized, _player.Rigidbody.transform.forward.normalized);
        
        // if the dot is less than 0.5, we need to rotate the body towards camera forward first
        if (dot2 < 0)
        {
            dir = _player.Camera.GetCameraYawTransform().forward;
        }
        
        // Avoid rotating to zero
        if(dir == Vector3.zero)
            return;
        
        // Rotate the body towards the direction
        RotateRigidbody(_player.Rigidbody, dir, 200f);
    }

    
}
