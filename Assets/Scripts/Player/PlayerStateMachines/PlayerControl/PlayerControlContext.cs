using RootMotion.Dynamics;
using RootMotion.FinalIK;
using System;
using UnityEngine;
using static RigidbodyMovement;
using static FootControlStateMachine;

public class PlayerControlContext
{
    // Constructor
    public PlayerControlContext(PlayerController player, FullBodyBipedIK _bodyIK, CapsuleCollider bodyCollider, SphereCollider fallCollider, LayerMask groundLayers,
        Foot leftFoot, Foot rightFoot, MovementSettings bodyMovementSettings, float stepLength, float stepHeight)
    {
        Player = player;
        LeftHandEffector = _bodyIK.solver.leftHandEffector;
        RightHandEffector = _bodyIK.solver.rightHandEffector;

        BodyCollider = bodyCollider;
        FallCollider = fallCollider;
        GroundLayers = groundLayers;
        LeftFoot = leftFoot;
        RightFoot = rightFoot;
        BodyMovementSettings = bodyMovementSettings;
        StepLength = stepLength;
        StepHeight = stepHeight;
    }

    // Read-only properties
    public PlayerController Player { get; }
    public IKEffector LeftHandEffector { get; }
    public IKEffector RightHandEffector { get; }
    public CapsuleCollider BodyCollider { get; }
    public SphereCollider FallCollider { get; }
    public Foot LeftFoot { get; }
    public Foot RightFoot { get; }
    public float StepLength { get; }
    public float StepHeight { get; }
    public LayerMask GroundLayers { get; }
    public MovementSettings BodyMovementSettings { get; }
    public float LowestFootPosition => Mathf.Min(LeftFoot.Position.y, RightFoot.Position.y);
    public EFallCondition FallCondition { get; private set; }
    public FallData Fall { get; private set; }
    public bool ShouldFall => GetShouldFall();
    public bool ShouldLeap => GetShouldLeap();

    // private fields
    private float _leapTimer;

    // Public methods
    public bool IsGrounded()
    {
        // Check if the feet are grounded using CheckSphere
        return Physics.CheckSphere(LeftFoot.Target.position, 0.8f, GroundLayers) ||
               Physics.CheckSphere(RightFoot.Target.position, 0.8f, GroundLayers);
    }
    
    public bool IsLiftingFoot(out Foot liftedFoot, out Foot plantedFoot) 
    {
        liftedFoot = LeftFoot.State == FootControlStateMachine.EFootState.Lifted ? LeftFoot : RightFoot;
        plantedFoot = LeftFoot.State == FootControlStateMachine.EFootState.Lifted ? RightFoot : LeftFoot;
        return LeftFoot.State == FootControlStateMachine.EFootState.Lifted || RightFoot.State == FootControlStateMachine.EFootState.Lifted;
    }

    public bool IsPlacingFoot(out Foot placingFoot, out Foot otherFoot)
    {
        placingFoot = LeftFoot.State == FootControlStateMachine.EFootState.Placing ? LeftFoot : RightFoot;
        otherFoot = LeftFoot.State == FootControlStateMachine.EFootState.Placing ? RightFoot : LeftFoot;
        return LeftFoot.State == FootControlStateMachine.EFootState.Placing || RightFoot.State == FootControlStateMachine.EFootState.Placing;
    }

    public Vector3 CalculatePelvisPoint(Vector3 pelvisOffset)
    {
        var legLength = 0.48f;
        var leftFootPos = LeftFoot.Target.position;
        var rightFootPos = RightFoot.Target.position;

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
            return Vector3.zero;
        }

        // Using the lowest foot as a base, and adding the offset. We also add the paramater offset with limit of 0f.
        float pelvisYPosition = Mathf.Min(leftFootPos.y, rightFootPos.y) + pelvisYOffset + Mathf.Min(pelvisOffset.y, 0f);
        Debug.DrawLine(Player.Rigidbody.position, new Vector3(pelvisOffset.x, pelvisYPosition, pelvisOffset.z), Color.green);
        return new Vector3(pelvisOffset.x, pelvisYPosition, pelvisOffset.z);

    }

    public void MoveToHipPoint(Vector3 pelvisOffset)
    {
        var targetPosition = CalculatePelvisPoint(pelvisOffset);
        Debug.DrawLine(Player.Rigidbody.position, targetPosition, Color.red);
        if (targetPosition == Vector3.zero)
            return;
        MoveToRigidbody(Player.Rigidbody, targetPosition, BodyMovementSettings);
    }
    
    public void MoveBody(Vector3 targetPosition)
    {
        // The combined velocity of the feet
        MoveToRigidbody(Player.Rigidbody, targetPosition, BodyMovementSettings, LeftFoot.Velocity + RightFoot.Velocity);
    }

    public Vector3 BetweenFeet(float lerp)
    {
        var pos = Vector3.Lerp(LeftFoot.Position, RightFoot.Position, lerp);
        return pos;
    }

    public float FeetLerp()
    {
        // Find out what foot is lifting
        var leftLift = LeftFoot.State == FootControlStateMachine.EFootState.Lifted;
        var rightLift = RightFoot.State == FootControlStateMachine.EFootState.Lifted;

        var dist = Vector3.Distance(LeftFoot.Target.position, RightFoot.Target.position);
        var start = StepLength * 0.7f;

        if (leftLift)
        {
            var lerp = dist - start;
            lerp /= StepLength;
            return Mathf.Lerp(0.8f, 0.5f, lerp);
        }

        if (rightLift)
        {
            var lerp = dist - start;
            lerp /= StepLength;
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
        var otherDir = Player.RelativeMoveInput;
        // Get the dot between camera and other direction
        var dot = Vector3.Dot(Player.Camera.GetCameraYawTransform().forward.normalized, otherDir.normalized);
        if (dot < -0.2f)
            otherDir = -otherDir;
        
        // Downwards lerp
        var angle = Player.Camera.CameraX;
        var dir = Vector3.Lerp(otherDir, direction, angle/60f);
        
        // Get the dot between the lerped direction and the player's forward direction
        var dot2 = Vector3.Dot(dir.normalized, Player.Rigidbody.transform.forward.normalized);
        
        // if the dot is less than 0.5, we need to rotate the body towards camera forward first
        if (dot2 < 0)
        {
            dir = Player.Camera.GetCameraYawTransform().forward;
        }
        
        // Avoid rotating to zero
        if(dir == Vector3.zero)
            return;
        
        // Rotate the body towards the direction
        RotateRigidbody(Player.Rigidbody, dir, 200f);
    }

    public void StopFeet()
    {
        LeftFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Stop);
        RightFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Stop);
    }
    public void StartFeet()
    {
        LeftFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Start);
        RightFoot.Sm.TransitionToState(FootControlStateMachine.EFootState.Start);
    }

    public enum EFallCondition
    { 
        Placing,
        Falling,
        Distance,
    }

    public struct FallData
    {
        public Foot PlaceFoot;
    }

    public void SetFallCondition(EFallCondition condition, FallData data = default)
    {
        FallCondition = condition;
        Fall = data;
    }

    // Conditions to enter fall state
    private bool _isTemporaryFall;
    private float _temporaryFallHeight;
    public bool GetShouldFall()
    {
        var data = new FallData();
        var isPlacing = IsPlacingFoot(out var placing, out var other);
        data.PlaceFoot = placing;
        // When the planted foot is higher grounded than the max possible height
        if (isPlacing && false)
            if (placing.Position.y < other.Position.y - StepHeight)
            {
                SetFallCondition(EFallCondition.Placing, data);
                return true;
            }

        // If the distance between the feet is too big, we fall
        if (Vector3.Distance(LeftFoot.Position, RightFoot.Position) > StepLength * 2)
        {
            SetFallCondition(EFallCondition.Distance, data);
            return true;
        }

        // When both feet are not planted, save the fall start height
        if (!LeftFoot.Planted && !RightFoot.Planted)
        {
            if (!_isTemporaryFall)
            {
                _isTemporaryFall = true;
                _temporaryFallHeight = Player.Transform.position.y;
            }
            // If we fall 1 meter or more, we set the fall condition to falling
            if ((_temporaryFallHeight - Player.Transform.position.y) > 2 && _isTemporaryFall)
            {
                SetFallCondition(EFallCondition.Falling);
                return true;
            }
        }
        else
        {
            _isTemporaryFall = false;
            _temporaryFallHeight = 0f;
        }

        return false;
    }

    /// <summary>
    /// This method is used to check if the player should consider or stop leaping.
    /// Another method is used to check if the player should actually leap.
    /// </summary>
    public bool CanLeap()
    {
        // If the feet state combination is lifted and placing, we want to leap
        if (IsLiftingFoot(out var lifted, out var other))
            return other.Placing;
        return false;
    }

    public bool GetShouldLeap()
    {
        if (CanLeap())
        {
            // If we can leap, we increment the timer
            _leapTimer += Time.deltaTime;
            // If the timer is great enough, we can leap
            if (_leapTimer > 0.2f)
            {
                return true;
            }
            return false;
        }

        // When we should not leap, we reset the timer
        _leapTimer = 0f;
        return false;
    }

    /// <summary>
    /// When we exit falling state, we need to reset the fall condition.
    /// </summary>
    public void ResetFall()
    {
        _isTemporaryFall = false;
        _temporaryFallHeight = 0f;
    }

    /// <summary>
    /// If the body is too far away from the feet.
    /// We need to turn off the body collider.
    /// </summary>
    public void HandleStretch()
    {
        var distToLeft = Vector3.Distance(Player.Rigidbody.position, LeftFoot.Position);
        var distToRight = Vector3.Distance(Player.Rigidbody.position, RightFoot.Position);
        var dist = Mathf.Min(distToLeft, distToRight);
        // If the distance is greater than the step length, we need to turn off the body collider
        if (dist > StepLength)
        {
            BodyCollider.enabled = false;
        }
        else
        {
            BodyCollider.enabled = true;
        }
    }

}
