using RootMotion.FinalIK;
using UnityEngine;
// DEPENDING ON WHAT CONTROL SYSTEMS ARE USED AND IN WHAT ORDER THEY ARE USED, THIS STATE MAY BE UNNECESSARY
public class StartSneakState : ProceduralSneakState
{
    public StartSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    // Private variables
    private bool _started;
    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (_started)
        {
            return ProceduralSneakStateMachine.ESneakState.Planted;
        }
        
        return StateKey;
    }

    public override void EnterState()
    {
        // Get the IK controls
        var leftFootIK = Context.BodyIK.solver.leftFootEffector;
        var rightFootIK = Context.BodyIK.solver.rightFootEffector;
        
        // Set the foot targets to the current positions
        Context.LeftFoot.position = leftFootIK.bone.position;
        Context.RightFoot.position = rightFootIK.bone.position;
        
        // Get the grounded positions
        var footPlaceOffset = Vector3.up * 0.05f;
        var groundCastUpOffset = Vector3.up * 0.1f;
        Context.LeftFoot.position = Context.GroundCast(Context.LeftFoot.position + groundCastUpOffset, 1f).point + footPlaceOffset;
        Context.RightFoot.position = Context.GroundCast(Context.RightFoot.position + groundCastUpOffset, 1f).point + footPlaceOffset;
        
        //Activate the foot targets
        leftFootIK.positionWeight = 1f;
        rightFootIK.positionWeight = 1f;
        
        _started = true;
    }

    public override void ExitState()
    {
        _started = false;
    }

    public override void UpdateState()
    {
        
    }

    public override void FixedUpdateState()
    {
        
    }
    
}