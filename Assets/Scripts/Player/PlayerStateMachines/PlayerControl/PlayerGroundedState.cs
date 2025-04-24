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
        Context.MoveToHipPoint(GetHipPosition());
        
        if(Context.Player.RelativeMoveInput != Vector3.zero)
            Context.UpdateBodyRotation(Context.Player.Camera.GetCameraYawTransform().forward);
    }

    private Collider[] _result = new Collider[10];
    private Vector3 GetHipPosition()
    {
        var pos = Context.BetweenFeet(0.5f);
        
        // If we are sneaking, we want to move the hip towards the planted foot
        if (Context.Player.IsSneaking)
        {
            // But only if we are lifting the foot
            if(Context.IsLiftingFoot(out var lifted, out var planted))
                pos = Vector3.Lerp(lifted.Position, planted.Position, 0.8f); // Slightly towards the planted foot
                
            // We also want a local avoidance if there are walls close to the body
            var player = Context.Player;
            // We need to check around the player if there are walls
            var size = Physics.OverlapSphereNonAlloc(player.EyePosition, 0.4f, _result, Context.GroundLayers);
            var dir = Vector3.zero;
            for (int i = 0; i < size; i++)
            {
                var col = _result[i];
                if (!col || col.isTrigger) continue;
                
                // We want to move away from the wall
                var wallPos = col.ClosestPoint(player.EyePosition);
                var wallDir = (wallPos - player.EyePosition);
                // we dont care about the y axis
                wallDir.y = 0;
                dir += wallDir;
            }

            if(dir != Vector3.zero)
                pos -= Vector3.Lerp(dir.normalized * 0.2f, Vector3.zero, (dir.magnitude / size) / 0.4f);
            
            // Now we check if we should duck under something
            // We cast a box from the player forward, and if it hits, we adjust the height of the hip
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
        }
        
        // The default hip position is the center of the feet
        return pos;
    }
}
