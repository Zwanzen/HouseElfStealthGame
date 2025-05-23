using RootMotion.Dynamics;
using UnityEngine;
using static PlayerControlContext;
using static PlayerAnimator;

public class PlayerFallingState : PlayerControlState
{
    public PlayerFallingState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }

    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        if (_canSwitchState)
            return PlayerControlStateMachine.EPlayerControlState.Grounded;

        return StateKey;
    }

    private bool _stopped;
    private float _timer;
    private bool _standingUp;
    private bool _canSwitchState;
    private Vector3 _fallDirection;
    private float _rotationSpeed = 5f;
    private AnimatorStateInfo _stateInfo;

    public override void EnterState()
    {
        // Call on fall event
        Context.Player.StartFall();

        // Subscribe to events
        SubscribeToEvents();
        // Get the body ready to fall
        SetBodyToFall();
        // Disable the feet
        Context.StopFeet();
        // Reset the animator triggers
        Context.Player.Animator.ResetTriggers();
        // Set the animation to fall
        Context.Player.Animator.SetAnim(EAnimType.Fall);

        // Logic Ready
        _stopped = false;
        _timer = 0f;
        _standingUp = false;
        _canSwitchState = false;

        // Get the fall direction used to rotate the body correctly
        _fallDirection = GetFallDirection();
        // We also add some force in that direction in certain conditions
        if (Context.FallCondition == EFallCondition.Placing || Context.FallCondition == EFallCondition.Distance)
            Context.Player.Rigidbody.AddForce(_fallDirection.normalized * 3f, ForceMode.VelocityChange);
    }

    public override void ExitState()
    {
        // Call on stop fall event
        Context.Player.StopFall();
        // Update the standing up state
        Context.Player.SetStandingUp(false);

        // Unsubscribe from events
        UnsubscribeFromEvents();
        // Ready the body to stand up
        SetBodyToStand();
        // Start the feet again
        Context.StartFeet();

        // Make sure to turn off the fall bool
        Context.Player.Animator.OffAnim(EAnimType.FallStop);
        // Reset triggers
        Context.Player.Animator.ResetTriggers();
        // Reset fall conditions
        Context.ResetFall();
    }


    public override void UpdateState()
    {
        if (!_stopped)
            CheckForStop();
        // If we are stopped, we want to stand up after 0.5 seconds
        else if (!_standingUp)
            CheckForStandUp();
    }

    public override void FixedUpdateState()
    {
        // If we are falling, and not stopped, we want to rotate the body towards the fall direction
        if (!_stopped)
            RotateBody(_fallDirection);
    }

    private void RotateBody(Vector3 direction)
    {
        // We want to rotate the body towards the direction on the y axis
        if (direction == Vector3.zero)
            return;
        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Context.Player.Rigidbody.MoveRotation(Quaternion.Slerp(Context.Player.Rigidbody.rotation, targetRotation, Time.fixedDeltaTime * _rotationSpeed));
    }

    private void CheckForStop()
    {
        // Check if we have stopped falling
        if (Context.Player.Rigidbody.linearVelocity.magnitude < 0.01f)
            _timer += Time.deltaTime;
        else
            _timer = 0f;

        if (_timer <= 0.15f)
            return;

        OnStop();
    }

    private void OnStop()
    {
        _stopped = true;
        // We can call trigger once stuff here
        SetBodyToStop();
        Context.Player.SetStandingUp(true);
        // We want to play the standing up animation
        Context.Player.Animator.SetAnim(EAnimType.FallStop);
        // We also want to store that animation state to know when we are done
        _stateInfo = Context.Player.Animator.CurrentAnimInfo;
        // We can re-use the timer for standing up
        _timer = 0f;
    }

    private void CheckForStandUp()
    {
        _timer += Time.deltaTime;
        if (_timer >= 0.5f)
        {
            Context.Player.Animator.SetAnim(EAnimType.FallStand);
            Context.Player.Animator.RegisterAnimationCallback(EAnimType.FallStand, OnStandUpComplete);
            _standingUp = true;
        }
    }

    // This should be called when the stand up animation is done
    private void OnStandUpComplete()
    {
        _canSwitchState = true;
    }

    private Vector3 GetFallDirection()
    {
        // Base fall direction
        var direction = Context.Player.Transform.forward;

        // If we placed wrongly, we need to add a force towards the placing foot
        if (Context.FallCondition == EFallCondition.Placing)
        {
            var dirToPlace = Context.Fall.PlaceFoot.Position - Context.Player.Rigidbody.transform.position;
            dirToPlace.y = 0f;

            if (dirToPlace != Vector3.zero)
            {
                // Set the rotation to point in that direction
                direction = dirToPlace;
            }
        }

        // If we just fall, we set the rotation to the fall direction
        if (Context.FallCondition == EFallCondition.Falling)
        {
            var dirVel = Context.Player.Rigidbody.linearVelocity;
            dirVel.y = 0f;
            // If the direction is not zero, we set the new direction
            if (direction == Vector3.zero)
                direction = dirVel;
        }

        // This would result in default direction
        //if (Context.FallCondition == EFallCondition.Distance) { }


        return direction;
    }

    private void SetBodyToFall()
    {
        Context.FallCollider.enabled = true;
        Context.BodyCollider.enabled = false;
        Context.Player.Rigidbody.useGravity = true;
    }

    private void SetBodyToStop()
    {
        Context.Player.Rigidbody.useGravity = false;
        Context.Player.Rigidbody.isKinematic = true;
    }

    private void SetBodyToStand()
    {
        // We also force the default animation to play
        //Context.Player.Animator.ForceAnim(EAnimType.Stealth);
        var pelvisPos = Context.FallCollider.transform.position + Vector3.up * 0.5f;
        //pelvisPos.y = 0f;
        //pelvisPos = Context.CalculatePelvisPoint(pelvisPos);
        Context.Player.Transform.position = pelvisPos;
        Context.Player.Rigidbody.isKinematic = false;
        Context.FallCollider.enabled = false;
        Context.BodyCollider.enabled = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision happend within a certain angle from the downward direction
        var angle = Vector3.Angle(collision.contacts[0].normal, Vector3.up);
        if (angle > 20f)
            return;

        if (collision.relativeVelocity.magnitude > 3f)
            Context.Player.Animator.SetAnim(EAnimType.FallHit);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Context.Player.Rigidbody.linearVelocity.magnitude < 0.25f)
            OnStop();
    }

    private void SubscribeToEvents()
    {
        Context.Player.Rigidbody.GetComponent<CollisionBroadcaster>().OnCollisionEnterEvent += OnCollisionEnter;
        Context.Player.Rigidbody.GetComponent<CollisionBroadcaster>().OnCollisionStayEvent += OnCollisionStay;

    }
    private void UnsubscribeFromEvents()
    {
        Context.Player.Rigidbody.GetComponent<CollisionBroadcaster>().OnCollisionEnterEvent -= OnCollisionEnter;
        Context.Player.Rigidbody.GetComponent<CollisionBroadcaster>().OnCollisionStayEvent -= OnCollisionStay;
    }
}