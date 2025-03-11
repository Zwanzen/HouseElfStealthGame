using UnityEngine;

public class StopSneakState : ProceduralSneakState
{
    public StopSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    // Private variables
    private bool _hasStopped;
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (_hasStopped)
        {
            return ProceduralSneakStateMachine.ESneakState.Idle;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        // Get the IK controls
        var leftFootIK = Context.BodyIK.solver.leftFootEffector;
        var rightFootIK = Context.BodyIK.solver.rightFootEffector;
        
        // Deactivate the foot targets
        leftFootIK.positionWeight = 0f;
        rightFootIK.positionWeight = 0f;
        
        _hasStopped = true;
    }

    public override void ExitState()
    {
        _hasStopped = false;
    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        
    }
}