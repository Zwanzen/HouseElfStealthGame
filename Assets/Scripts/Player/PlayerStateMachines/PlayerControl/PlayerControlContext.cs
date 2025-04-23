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

    public void MoveToHipPoint()
    {
        var legLength = 0.48f;
        var leftFootPos = _leftFoot.Target.position;
        var rightFootPos = _rightFoot.Target.position;
        var offsetPos = Vector3.zero;
        var isLifting = _leftFoot.State == FootControlStateMachine.EFootState.Lifted || _rightFoot.State == FootControlStateMachine.EFootState.Lifted;

        
        var feetMidpoint = (leftFootPos + rightFootPos) * 0.5f;
        Vector3 pelvisHorizontalPosition = new Vector3(feetMidpoint.x, 0f, feetMidpoint.z);
        if (isLifting && _player.IsSneaking)
        {
            var liftedFoot = _leftFoot.State == FootControlStateMachine.EFootState.Lifted ? _leftFoot.Target.position : _rightFoot.Target.position;
            var plantedFoot = _leftFoot.State == FootControlStateMachine.EFootState.Lifted ? _rightFoot.Target.position : _leftFoot.Target.position;
            pelvisHorizontalPosition = Vector3.Lerp(liftedFoot,plantedFoot, 0.8f);
        }
        float horizontalDistance = Vector2.Distance(new Vector2(leftFootPos.x, leftFootPos.z), new Vector2(rightFootPos.x, rightFootPos.z));
        float halfHorizontalDistance = horizontalDistance * 0.5f;

        float leftLegVerticalOffsetSquared = legLength * legLength - halfHorizontalDistance * halfHorizontalDistance;
        float rightLegVerticalOffsetSquared = legLength * legLength - halfHorizontalDistance * halfHorizontalDistance;

        float pelvisYOffset = 0f;
        if (leftLegVerticalOffsetSquared >= 0 && rightLegVerticalOffsetSquared >= 0)
        {
            pelvisYOffset = Mathf.Min(Mathf.Sqrt(leftLegVerticalOffsetSquared), Mathf.Sqrt(rightLegVerticalOffsetSquared));
        }
        else if (leftLegVerticalOffsetSquared >= 0)
        {
            pelvisYOffset = Mathf.Sqrt(leftLegVerticalOffsetSquared);
        }
        else if (rightLegVerticalOffsetSquared >= 0)
        {
            pelvisYOffset = Mathf.Sqrt(rightLegVerticalOffsetSquared);
        }
        else
        {
            Debug.LogWarning("Leg lengths are too short for the given foot positions!");
            // Handle the case where no valid pelvis position exists
            return;
        }

        float pelvisYPosition = Mathf.Min(leftFootPos.y, rightFootPos.y) + pelvisYOffset;
        Vector3 pelvisPosition = new Vector3(pelvisHorizontalPosition.x, pelvisYPosition, pelvisHorizontalPosition.z);
        
        var bodyPos = pelvisPosition;
        Debug.DrawLine(_player.Rigidbody.position, bodyPos, Color.red);
        MoveToRigidbody(_player.Rigidbody, bodyPos, _bodyMovementSettings);
    }

    // Credit: https://youtu.be/qdskE8PJy6Q?si=hSfY9B58DNkoP-Yl
    // Modified
    public void RigidbodyFloat()
    {
        var vel = _rigidbody.linearVelocity;
        var relVel = Vector3.Dot(Vector3.down, vel);
        
        var leftFootPos = _leftFoot.Target.position;
        var rightFootPos = _rightFoot.Target.position;

        // --- Calculate Horizontal Positioning ---

        // 1. Find the midpoint between the feet (horizontally)
        float midX = (leftFootPos.x + rightFootPos.x) * 0.5f;
        float midZ = (leftFootPos.z + rightFootPos.z) * 0.5f;

        // 2. Calculate the squared horizontal distance from the midpoint to one foot
        // (Using left foot here, distance to right foot would be the same)
        float horizontalDeltaX = midX - leftFootPos.x;
        float horizontalDeltaZ = midZ - leftFootPos.z;
        // This is the squared length of the base of our right triangle
        float horizontalDistSq = (horizontalDeltaX * horizontalDeltaX) + (horizontalDeltaZ * horizontalDeltaZ);
        // Alternative way: squared horizontal distance between feet / 4
        // float feetHorizontalDistX = leftFootPos.x - rightFootPos.x;
        // float feetHorizontalDistZ = leftFootPos.z - rightFootPos.z;
        // float horizontalDistSq = (feetHorizontalDistX * feetHorizontalDistX + feetHorizontalDistZ * feetHorizontalDistZ) * 0.25f;

        // --- Calculate Vertical Positioning ---

        // 3. Calculate the squared vertical component using Pythagorean theorem: leg² = horizontal² + vertical²
        // vertical² = leg² - horizontal²
        float legLengthSq = 0.45f * 0.45f;
        float verticalDistSq = legLengthSq - horizontalDistSq;

        // 4. Handle impossible positions (feet too far apart horizontally for leg length)
        // If verticalDistSq is negative, the feet are further apart horizontally than the leg can reach.
        // In this case, the leg must be fully stretched horizontally (vertical component is zero).
        if (verticalDistSq < 0)
        {
            verticalDistSq = 0;
            // Optional: Could add a debug warning here if feet are often forced too far apart
            // Debug.LogWarning("Feet positions are further apart than leg length allows.");
        }

        // 5. Calculate the actual vertical offset (height difference between hip and foot along that leg's triangle)
        float verticalOffset = Mathf.Sqrt(verticalDistSq);

        // 6. Determine the base height from the highest foot
        // The hip needs to be high enough to allow the leg connected to the *higher* foot to reach.
        float highestFootY = Mathf.Max(leftFootPos.y, rightFootPos.y);

        // 7. Calculate the final target hip height
        float targetHipHeightY = highestFootY + verticalOffset;
        
        var x = targetHipHeightY - 1f; // This is where the final height is set
        Debug.DrawLine(_player.Rigidbody.position, _player.Rigidbody.position + Vector3.up * x, Color.green);
        
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
        //MoveRigidbody(Player.Rigidbody, dir, _bodyMovementSettings);
        MoveToRigidbody(_player.Rigidbody, targetPosition, _bodyMovementSettings);
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
        var leftLift = _leftFoot.State == FootControlStateMachine.EFootState.Lifted;
        var rightLift = _rightFoot.State == FootControlStateMachine.EFootState.Lifted;

        var dist = Vector3.Distance(_leftFoot.Target.position, _rightFoot.Target.position);
        var start = _player.StepLength * 0.7f;

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

    public void StopFeet()
    {
        _leftFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Stop);
        _rightFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Stop);
    }
    public void StartFeet()
    {
        _leftFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Start);
        _rightFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Start);
    }
    
}
