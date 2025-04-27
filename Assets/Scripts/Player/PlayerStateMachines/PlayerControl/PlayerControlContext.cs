using RootMotion.Dynamics;
using RootMotion.FinalIK;
using System;
using UnityEngine;
using static RigidbodyMovement;

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
        return new Vector3(pelvisOffset.x, pelvisYPosition, pelvisOffset.z);

    }

    public void MoveToHipPoint(Vector3 pelvisOffset)
    {
        var targetPosition = CalculatePelvisPoint(pelvisOffset);
        if (targetPosition == Vector3.zero)
            return;
        MoveToRigidbody(Player.Rigidbody, targetPosition, BodyMovementSettings);
    }
    
    public void MoveBody(Vector3 targetPosition)
    {
        MoveToRigidbody(Player.Rigidbody, targetPosition, BodyMovementSettings);
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

    public void SetFallCondition(EFallCondition condition)
    {
        FallCondition = condition;
        var fallData = new FallData();
        if (condition == EFallCondition.Placing)
            fallData.PlaceFoot = LeftFoot.Placing ? LeftFoot : RightFoot;

            Fall = fallData;
    }

}
