﻿using UnityEngine;
using static RigidbodyMovement;

public class PlantedSneakState : ProceduralSneakState
{
    public PlantedSneakState(ProceduralSneakContext context, ProceduralSneakStateMachine.ESneakState key) : base(context, key)
    {
        Context = context;
    }
    
    // Private variables

    
    public override ProceduralSneakStateMachine.ESneakState GetNextState()
    {
        if (!Context.Player.IsGrounded)
        {
            return ProceduralSneakStateMachine.ESneakState.Stop;
        }

        if (InputManager.Instance.IsHoldingLMB)
        {
            Context.LiftedFoot = Context.LeftFoot;
            return ProceduralSneakStateMachine.ESneakState.Lifted;
        }
        
        if (InputManager.Instance.IsHoldingRMB)
        {
            Context.LiftedFoot = Context.RightFoot;
            return ProceduralSneakStateMachine.ESneakState.Lifted;
        }
        
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
        UpdateFeetVelocities();
        HandleBodyPosition();
        Context.UpdateFootGroundNormal(Context.LeftFoot);
        Context.UpdateFootGroundNormal(Context.RightFoot);
    }

    private void HandleBodyPosition()
    {
        var pos = Context.GetFeetMiddlePoint();
        pos += Context.Player.RelativeMoveInput.normalized * 0.05f;
        Context.MoveBody(pos);
    }

    // Moves both feet to their grounded positions
    private void UpdateFeetVelocities()
    {
        // Get the current foot ground positions
        var leftFootGroundPos = Context.GetFootGroundPosition(Context.LeftFoot);
        var rightFootGroundPos = Context.GetFootGroundPosition(Context.RightFoot);
        
        // Get the direction to the foot ground positions
        var leftDirection = leftFootGroundPos - Context.LeftFoot.position;
        var rightDirection = rightFootGroundPos - Context.RightFoot.position;
        
        // Move the feet to their grounded positions
        MoveRigidbody(Context.LeftFoot, leftDirection, Context.PlantedMovementSettings);
        MoveRigidbody(Context.RightFoot, rightDirection, Context.PlantedMovementSettings);
    }
    
}