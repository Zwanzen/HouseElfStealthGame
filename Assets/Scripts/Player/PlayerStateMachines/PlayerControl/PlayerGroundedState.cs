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
            var playerRb = Context.Player.Rigidbody;
            // We need to check around the player if there are walls
            var size = Physics.OverlapSphereNonAlloc(playerRb.position, 0.4f, _result, Context.GroundLayers);
            var dir = Vector3.zero;
            for (int i = 0; i < size; i++)
            {
                var col = _result[i];
                if (!col || col.isTrigger) continue;
                
                // We want to move away from the wall
                var wallPos = col.ClosestPoint(playerRb.position);
                var wallDir = (wallPos - playerRb.position);
                wallDir.y = 0;
                dir = Vector3.Lerp(wallDir.normalized * 0.15f, Vector3.zero, wallDir.magnitude / 0.3f);
            }
            
            pos -= dir; // The amount we want to move away from the wall
        }
        
        // The default hip position is the center of the feet
        return pos;
    }
}
