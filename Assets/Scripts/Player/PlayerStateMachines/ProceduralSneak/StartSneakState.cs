using RootMotion.FinalIK;
using UnityEngine;

public class StartSneakState : ProceduralSneakState
{
    public StartSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    // Private variables
    private bool _hasReset;
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        return StateKey;
    }

    public override void EnterState()
    {

        // Activate all IKs
        var leftFootIK = Context.BodyIK.solver.leftFootEffector;
        var rightFootIK = Context.BodyIK.solver.rightFootEffector;
        leftFootIK.positionWeight = 1f;
        leftFootIK.rotationWeight = 1f;


    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        
    }
    
}