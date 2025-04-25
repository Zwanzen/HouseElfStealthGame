using Unity.Mathematics;
using UnityEngine;

public class PlayerGroundedState : PlayerControlState
{
    public PlayerGroundedState(PlayerControlContext context, PlayerControlStateMachine.EPlayerControlState key) : base(context, key)
    {
        Context = context;
    }


    public override PlayerControlStateMachine.EPlayerControlState GetNextState()
    {
        return StateKey;
    }

    public override void EnterState()
    {
    }


    public override void ExitState()
    {

    }

    private float _timer;
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
            if(Context.IsLiftingFoot(out var lifted, out var planted))
                pos = Vector3.Lerp(lifted.Position, planted.Position, 0.8f); // Slightly towards the planted foot
                
            var player = Context.Player; // For convinience

            // Now we check if we should duck under something
            // We cast a box from the player forward, and if it hits, we adjust the height of the hip
            // Make sure the y offset is not set before this
            pos.y = 0f;
            if (Physics.BoxCast(player.Rigidbody.position + player.Camera.GetCameraYawTransform().forward * player.Collider.radius, 
                    new Vector3(player.Collider.radius, player.Collider.radius, player.Collider.radius + player.Collider.radius),
                    Vector3.up, out var hit, Quaternion.Euler(0, player.Camera.GetCameraYawTransform().rotation.eulerAngles.y, 0),
                    (Context.LowestFootPosition + PlayerController.Height) - player.Rigidbody.position.y, Context.GroundLayers))
            {
                // Now if we should duck, we add the difference to the hip position
                if(hit.point.y < Context.LowestFootPosition + PlayerController.Height)
                {
                    // We want to move the hip down based on the hit point
                    var diff = Context.LowestFootPosition + PlayerController.Height - hit.point.y;
                    // If the difference is too big, we dont want to move the hip down
                    if (diff < 0.35f)
                        pos.y -= diff;
                }
                // For easy debugging
                Physics.CheckSphere(
                    new Vector3(hit.point.x, (Context.LowestFootPosition + PlayerController.Height), hit.point.z),
                    0.01f);
                Physics.CheckSphere(hit.point, 0.01f);
            }

            // We also want a local avoidance if there are walls close to the body
            // We need to check around the player if there are walls
            // We use the current calculated pelvis position to check for walls
            var calculatedPelvisPos = Context.CalculatePelvisPoint(pos);
            var size = Physics.OverlapSphereNonAlloc(calculatedPelvisPos, 0.6f, _result, Context.GroundLayers);
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
}
