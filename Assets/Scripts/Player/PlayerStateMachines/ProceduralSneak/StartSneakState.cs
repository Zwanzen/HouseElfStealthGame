﻿using RootMotion.FinalIK;
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
        Context.LeftFoot.linearVelocity = Vector3.zero;
        Context.RightFoot.linearVelocity = Vector3.zero;
        
        // Set the foot targets to the current positions
        Context.LeftFoot.position = Context.LeftFootRestTarget.position;
        Context.RightFoot.position = Context.RightFootRestTarget.position;
        
        // Set rotation to camera forward
        Quaternion forward = Quaternion.LookRotation(Context.Player.Camera.GetCameraYawTransform().forward, Vector3.up);
        Context.LeftFoot.rotation = forward;
        Context.RightFoot.rotation = forward;
        
        // Activate the foot target rotation
        leftFootIK.rotationWeight = 1f;
        rightFootIK.rotationWeight = 1f;
        
        // Set the grounded positions
        Context.LeftFoot.position = Context.GetFootGroundPosition(Context.LeftFoot);
        Context.RightFoot.position = Context.GetFootGroundPosition(Context.RightFoot);
        
        // Activate the foot targets
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