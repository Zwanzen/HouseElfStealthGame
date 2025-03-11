using UnityEngine;

public class PlacingSneakState : ProceduralSneakState
{
    public PlacingSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }

    private bool _placed;
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (_placed)
        {
            return ProceduralSneakStateMachine.ESneakState.Planted;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        Context.LiftedFoot.position = Context.GetFootGroundPosition(Context.LiftedFoot);
        
        _placed = true;
    }

    public override void ExitState()
    {
        _placed = false;
    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        
    }
}