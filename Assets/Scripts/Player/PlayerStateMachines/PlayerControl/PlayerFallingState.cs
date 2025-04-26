using RootMotion.Dynamics;
using UnityEngine;
using static PlayerControlContext;

public class PlayerFallingState : PlayerControlState
{
    public PlayerFallingState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        if(_canStandUp)
            return PlayerControlStateMachine.EPlayerControlState.Grounded;

        return StateKey;
    }

    private bool _stopped;
    private float _timer;
    private Vector3 _stoppedPos;
    private bool _canStandUp = false;
    private Quaternion _fallRotation;

    public override void EnterState()
    {
        Context.FallCollider.enabled = true;
        Context.BodyCollider.enabled = false;
        Context.Player.Rigidbody.useGravity = true;
        _stopped = false;
        _timer = 0f;
        _canStandUp = false;

        // If we placed wrongly, we need to add a force towards the placing foot
        if (Context.FallCondition == EFallCondition.Placing)
        {
            var direction = Context.Fall.PlaceFoot.Position - Context.Player.Rigidbody.transform.position;
            if(direction != Vector3.zero)
            {
                // set the rotation to point in that direction
                direction.y = 0f;
                _fallRotation = Quaternion.LookRotation(Vector3.down, direction);

                direction.y = 0.1f;
                Context.Player.Rigidbody.AddForce(direction.normalized * 1f, ForceMode.VelocityChange);
            }
        }

        // If we just fall, we set the rotation to the fall direction
        if (Context.FallCondition == EFallCondition.Falling)
        {
            var direction = Context.Player.Rigidbody.transform.position - Context.Player.Rigidbody.transform.position;
            direction.y = 0f;
            // If the direction is zero, we set the up direction to the player forward direction
            if (direction == Vector3.zero)
                direction = Context.Player.Transform.forward;
            _fallRotation = Quaternion.LookRotation(Vector3.down, direction);
        }

        // If the distance is too big, we set the rotation to just look down
        if (Context.FallCondition == EFallCondition.Distance)
        {
            _fallRotation = Quaternion.LookRotation(Vector3.down, Context.Player.Transform.forward);
        }

        Context.StopFeet();
    }

    public override void ExitState()
    {
        // Set rotation to 0
        Context.Player.Rigidbody.transform.rotation = Quaternion.Euler(0f, Context.Player.Rigidbody.transform.rotation.eulerAngles.y, 0f);
        Context.StartFeet();
        Context.Player.SetStandingUp(false);

        Context.LeftHandEffector.positionWeight = 0f;
        Context.RightHandEffector.positionWeight = 0f;
    }


    public override void UpdateState()
    {

        if(!_stopped)
            CheckForStop();

        Physics.CheckSphere(Context.LeftFoot.FootBonePosition, 0.05f);
        Physics.CheckSphere(Context.RightFoot.FootBonePosition, 0.05f);
    }

    public override void FixedUpdateState()
    {
        if (_stopped)
        {
            RigidbodyMovement.MoveToRigidbody(Context.Player.Rigidbody, _stoppedPos + Vector3.up * 0.4f, Context.BodyMovementSettings);
            // Rotate the player x rotation towards 0
            var rotationSpeed = 5f;
            var targetRotation = Quaternion.Euler(0f, Context.Player.Rigidbody.rotation.eulerAngles.y, 0f);
            Context.Player.Rigidbody.rotation = Quaternion.Slerp(Context.Player.Rigidbody.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // Check if we are standing up
            if (Context.Player.Rigidbody.rotation.eulerAngles.x < 1f)
                _canStandUp = true;

            // Slerp the hand effector weights towards 1
            Context.LeftHandEffector.positionWeight = Mathf.Lerp(Context.LeftHandEffector.positionWeight, 0f, Time.deltaTime * 5f);
            Context.RightHandEffector.positionWeight = Mathf.Lerp(Context.RightHandEffector.positionWeight, 0f, Time.deltaTime * 5f);

        }
        else
        {
            // Rotate the player towards the fall direction
            var rotationSpeed = 5f;
            var targetRotation = Quaternion.Slerp(Context.Player.Rigidbody.rotation, _fallRotation, Time.deltaTime * rotationSpeed);
            Context.Player.Rigidbody.MoveRotation(targetRotation);

            // Slerp the hand effector weights towards 1
            Context.LeftHandEffector.positionWeight = Mathf.Lerp(Context.LeftHandEffector.positionWeight, 0.8f, Time.deltaTime * 5f);
            Context.RightHandEffector.positionWeight = Mathf.Lerp(Context.RightHandEffector.positionWeight, 0.8f, Time.deltaTime * 5f);
        }
    }

    private void CheckForStop()
    {
        // Check if we have stopped falling
        if (Context.Player.Rigidbody.linearVelocity.magnitude < 0.01f)
            _timer += Time.deltaTime;
        else
            _timer = 0f;

        if (_timer >= 1f)
            _stopped = true;

        if (_stopped)
        {
            // We can call trigger once stuff here
            Context.Player.Rigidbody.useGravity = false;
            Context.FallCollider.enabled = false;
            Context.BodyCollider.enabled = true;
            _stoppedPos = Context.Player.Rigidbody.position;
            Context.Player.SetStandingUp(true);
        }
    }
}
