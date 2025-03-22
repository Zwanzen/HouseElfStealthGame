using System;
using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using Pathfinding;
using UnityEngine;
using static RigidbodyMovement;

namespace GOAP.Behaviors
{
    [RequireComponent(typeof(Rigidbody), typeof(AgentBehaviour), typeof(Seeker))]
    public class AgentMoveBehavior : MonoBehaviour
    {

        [SerializeField] private MovementSettings _movementSettings;
        
        private Rigidbody _rigidbody;
        private AgentBehaviour _agentBehaviour;
        private Seeker _seeker;

        private Path _path;
        private int _pathIndex;
        private bool _hasPath;
        
        private const float DistanceThreshold = 0.1f;
        
        private void Awake()
        {
            _agentBehaviour = GetComponent<AgentBehaviour>();
            _rigidbody = GetComponent<Rigidbody>();
            _seeker = GetComponent<Seeker>();
        }

        private void OnEnable()
        {
            _agentBehaviour.Events.OnTargetChanged += OnTargetChanged;
        }
        
        private void OnTargetChanged(ITarget target, bool inRange)
        {
            _hasPath = false;
            _pathIndex = 0;
            _seeker.StartPath(_rigidbody.position, target.Position, OnPathComplete);
        }
        
        private void OnPathComplete(Path path)
        {
            _path = path;
            _hasPath = true;
        }

        private void FixedUpdate()
        {
            if (_hasPath)
                HandleMovement();
        }
        
        Vector3 _storedGoalVelocity;
        private void HandleMovement()
        {
            // Check if we are close to the waypoint
            Vector3 waypoint = _path.vectorPath[_pathIndex];
            var dist = Vector3.Distance(_rigidbody.position, waypoint);
            if (dist < DistanceThreshold)
            {
                _pathIndex++;
                waypoint = _path.vectorPath[_pathIndex];
            }
            // Move towards the waypoint
            Vector3 direction = (waypoint - _rigidbody.position).normalized;
            _storedGoalVelocity = MoveRigidbody(_rigidbody, direction, _storedGoalVelocity, _movementSettings);
        }

        private void OnDisable()
        {
            _agentBehaviour.Events.OnTargetChanged -= OnTargetChanged;
        }
    }
}