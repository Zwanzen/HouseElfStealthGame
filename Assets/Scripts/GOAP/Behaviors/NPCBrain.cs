using System;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using GOAP.Goals;
using UnityEngine;

namespace GOAP.Behaviors
{
    [RequireComponent(typeof(AgentBehaviour))]
    public class NPCBrain : MonoBehaviour
    {
        private AgentBehaviour _agentBehaviour;
        private GoapActionProvider _actionProvider;

        private void Awake()
        {
            _agentBehaviour = GetComponent<AgentBehaviour>();
        }

        private void Start()
        {
            // Set goal to Wander
            _actionProvider.RequestGoal<PatrolGoal>();
        }
    }
}