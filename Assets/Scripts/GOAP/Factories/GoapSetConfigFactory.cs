using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using GOAP.Actions;
using GOAP.Goals;
using GOAP.Sensors;
using GOAP.Targets;
using GOAP.WorldKeys;

namespace GOAP.Factories
{
    public class GoapSetConfigFactory : CapabilityFactoryBase
    {
        public override ICapabilityConfig Create()
        {
            CapabilityBuilder builder = new("NPCSet");

            BuildGoals(builder);
            BuildActions(builder);
            BuildSensors(builder);
            
            return builder.Build();
        }
        
        private void BuildGoals(CapabilityBuilder builder)
        {
            builder.AddGoal<PatrolGoal>()
                .AddCondition<IsPatrolling>(Comparison.GreaterThanOrEqual,1);
        }
        
        private void BuildActions(CapabilityBuilder builder)
        {
            builder.AddAction<PatrolAction>()
                .SetTarget<PatrolTarget>()
                .AddEffect<IsPatrolling>(EffectType.Increase)
                .SetBaseCost(5)
                .SetStoppingDistance(0.1f);
        }
        
        private void BuildSensors(CapabilityBuilder builder)
        {
            builder.AddTargetSensor<PatrolTargetSensor>()
                .SetTarget<PatrolTarget>();
        }
    }
}