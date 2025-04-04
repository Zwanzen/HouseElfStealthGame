using UnityEngine;

public class FootFallingState : FootControlState
{
    public FootFallingState(FootControlContext context, FootControlStateMachine.EFootState key) : base(context, key)
    {
        Context = context;
    }
    
    public override FootControlStateMachine.EFootState GetNextState()
    {
        return Context.IsFootGrounded ? FootControlStateMachine.EFootState.Planted : StateKey;
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
        var dirToRest = new Vector3(Context.Foot.RestTarget.position.x, 0, Context.Foot.RestTarget.position.z);
        dirToRest += Vector3.down;
        Context.MoveFootToPosition(Vector3.down);
    } 
}