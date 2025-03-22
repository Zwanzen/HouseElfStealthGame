using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace GOAP.Sensors
{
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        public override void Created()
        {
        }

        public override void Update()
        {
        }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            Vector3 position = GetRandomPosition(agent);
            return new PositionTarget(position);
        }

        private Vector3 GetRandomPosition(IActionReceiver agent)
        {
            var random = Random.insideUnitCircle * 5f;
            var randomPosition = new Vector3(random.x, 0, random.y) + agent.Transform.position;
            return randomPosition;
        }
    }
}