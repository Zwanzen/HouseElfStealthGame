using UnityEngine;

public class FootLiftedState : FootControlState
{
    public FootLiftedState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }

    public override FootControlStateMachine.EFootState GetNextState()
    {
        if (!Context.IsFootLifting)
            return FootControlStateMachine.EFootState.Placing;
        
        return StateKey;
    }
    
    public override void EnterState()
    {
    }

    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
        var footPos = Context.Foot.Target.position;
        var otherFootPos = Context.OtherFoot.Target.position;

        // We want the foot to move upwards as much as possible first
        var wantedHeight = otherFootPos.y + 0.15f;
        var wantedHeightPos = new Vector3(footPos.x, wantedHeight, footPos.z);
        
        // We also calculate the wanted position based on the player input
        // But also take into account the wanted height
        var wantedPos = wantedHeightPos + Context.Player.RelativeMoveInput.normalized;
        
        // Depending on our distance to the wanted height compared to our current height
        // We lerp what position we want to go to
        var currentHeight = footPos.y - otherFootPos.y;
        var maxHeight = wantedHeight - otherFootPos.y;
        var posLerp = currentHeight / maxHeight;
        var pos = Vector3.Lerp(wantedHeightPos, wantedPos, posLerp);
        
        // We get the direction towards the wanted position
        var dir = pos - footPos;
        
        // Before we move, we change the dir magnitude based on the current one
        // This will keep the speed based on distance and curve
        var mag = dir.magnitude;
        var breakDistance = 0.1f;
        var magLerp = mag / breakDistance;
        dir.Normalize();
        dir *= Context.SpeedCurve.Evaluate(magLerp);
        
        Debug.DrawLine(footPos, footPos + dir, Color.red);
        Debug.Log($"Pos Lerp: {posLerp:F2}, Mag Lerp: {magLerp:F2}");
        
        Context.MoveFootToPosition(dir);
    }
}