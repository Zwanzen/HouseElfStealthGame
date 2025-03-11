using UnityEngine;

public class ProceduralSneakStateMachine : StateMachine<ProceduralSneakStateMachine.ESneakState>
{
       public enum ESneakState
       {
              Idle,
              Start,
              Stop,
              Planted,
              Lifted,
              Placing
       }
       
       private ProceduralSneakContext _context;
       
       // Read only properties
       public ESneakState State => CurrentState.StateKey;
       
       private void InitializeStates()
       {
              States.Add(ESneakState.Idle, new IdleSneakState(_context, ESneakState.Idle));
              States.Add(ESneakState.Start, new StartSneakState(_context, ESneakState.Start));
              States.Add(ESneakState.Stop, new StopSneakState(_context, ESneakState.Stop));
              States.Add(ESneakState.Planted, new PlantedSneakState(_context, ESneakState.Planted));
              States.Add(ESneakState.Lifted, new LiftedSneakState(_context, ESneakState.Lifted));
              States.Add(ESneakState.Placing, new PlacingSneakState(_context, ESneakState.Placing));

              CurrentState = States[ESneakState.Idle];
       }
       
       public void SetContext(ProceduralSneakContext context)
       {
              _context = context;
              InitializeStates();
       }
}