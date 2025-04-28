using UnityEngine;

public class PlayerGroundedState : PlayerControlState
{
    public PlayerGroundedState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }


    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        if(Context.ShouldFall)
            return PlayerControlStateMachine.EPlayerControlState.Falling;

        if (Context.ShouldLeap)
            return PlayerControlStateMachine.EPlayerControlState.Leap;


        return StateKey;
    }

    public override void EnterState()
    {
        // If we just fell, we would try to fall again unless we reset the fall condition
        //_isFalling = false;
    }


    public override void ExitState()
    {

    }

    public override void UpdateState()
    {

    }

    public override void FixedUpdateState()
    {
        Context.MoveToHipPoint(GetHipPositionOffset());
        
        if(Context.Player.RelativeMoveInput != Vector3.zero)
            Context.UpdateBodyRotation(Context.Player.Camera.GetCameraYawTransform().forward);
    }

    private Collider[] _result = new Collider[10];
    private Vector3 GetHipPositionOffset()
    {
        // We set the initial position to the center of the feet
        var pos = Context.BetweenFeet(0.5f);
        // We also set the y position to 0,
        // because the initial height is calculated in the CalculatePelvisPoint method.
        // But we can still add an offset to the y position.
        pos.y = 0f;

        // If we are sneaking, we want to move the hip towards the planted foot
        if (Context.Player.IsSneaking)
        {
            // But only if we are lifting the foot
            if (Context.IsLiftingFoot(out var lifted, out var planted))
                pos = Vector3.Lerp(lifted.Position, planted.Position, 0.8f); // Slightly towards the planted foot
            pos.y = 0f; // This offset should be set below.


            // Add the hip height offset
            pos.y += GetHipHeight();

            // We also want a local avoidance if there are walls close to the body
            // We need to check around the player if there are walls
            // We use the current calculated pelvis position to check for walls
            var normalPelvisPos = Context.CalculatePelvisPoint(pos);
            var size = Physics.BoxCastNonAlloc(normalPelvisPos, 0.6f, _result, Context.GroundLayers);
            var dir = Vector3.zero;
            var closestMag = 0f;
            for (int i = 0; i < size; i++)
            {
                var col = _result[i];
                if (!col || col.isTrigger) continue;

                // We want to move away from the wall
                var wallPos = col.ClosestPoint(calculatedPelvisPos);
                var wallDir = (wallPos - calculatedPelvisPos);

                if (i == 0)
                {
                    closestMag = wallDir.magnitude;
                }
                else
                {
                    if (wallDir.magnitude < closestMag)
                        closestMag = wallDir.magnitude;
                }
                // we dont care about the y axis
                wallDir.y = 0;
                dir += wallDir;
            }

            if (dir != Vector3.zero && false)
                pos -= Vector3.Lerp(dir.normalized * 0.2f, Vector3.zero, closestMag/0.6f);

        }

        // The default hip position is the center of the feet
        return pos;
    }

    private float GetHipHeight()
    {
        // For convinience
        var player = Context.Player;
        var radius = Context.BodyCollider.radius;

        // Now we check if we should duck under something
        // We cast a box from the player forward, and if it hits, we adjust the height of the hip
        if (Physics.BoxCast(player.Rigidbody.position + player.Camera.GetCameraYawTransform().forward * radius,
                new Vector3(radius, radius, radius + radius),
                Vector3.up, out var hit, Quaternion.Euler(0, player.Camera.GetCameraYawTransform().rotation.eulerAngles.y, 0),
                (Context.LowestFootPosition + PlayerController.Height) - player.Rigidbody.position.y, Context.GroundLayers))
        {

            // If we do hit something in front and above us, we need to check if we have space in front of us to duck to allow ducking
            // But only if we dont have anything above us at the time, then we are already ducking, and therefore should continue ducking
            var shouldDuck = true;
            // Check if we are already ducking
            var ducking = Physics.SphereCast(player.Rigidbody.position, radius * 0.9f, Vector3.up, out var upHit,
                (Context.LowestFootPosition + PlayerController.Height) - player.Rigidbody.position.y - (radius * 2), Context.GroundLayers);

            // If we are not ducking, check if we have space in front of us to duck
            if (!ducking)
            {
                var xzPos = player.Rigidbody.position + player.Camera.GetCameraYawTransform().forward * (radius * 2);
                var boxHeight = (hit.point.y - (Context.LowestFootPosition + 0.02f)) / 2;
                var yPos = hit.point.y - (boxHeight + 0.02f);
                var boxPos = new Vector3(xzPos.x, yPos, xzPos.z);

                // Create a overlap capsule that simulates the player's body collider, but in front of the player
                var overlap = Physics.OverlapBoxNonAlloc(boxPos,
                    new Vector3(radius, boxHeight, radius * 2), _result, player.Camera.GetCameraYawTransform().rotation, Context.GroundLayers);

                if (overlap > 0)
                    shouldDuck = false;
            }


            // Now if we should duck, we add the difference to the hip position
            if (hit.point.y < Context.LowestFootPosition + PlayerController.Height && shouldDuck)
            {
                // We want to move the hip down based on the hit point
                var diff = Context.LowestFootPosition + PlayerController.Height - hit.point.y;
                // If the difference is too big, we dont want to move the hip down
                if (diff < 0.38f)
                    return -diff;
            }
        }

        // If we are not ducking, we return the default height
        return 0;
    }
}
