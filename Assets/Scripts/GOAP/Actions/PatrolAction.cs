using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;

namespace GOAP.Actions
{
    public class PatrolAction : GoapActionBase<CommonData>
    {
        public override void Start(IMonoAgent agent, CommonData data)
        {
            data.Timer = 10f;
        }
        
        public override IActionRunState Perform(IMonoAgent agent, CommonData data, IActionContext context)
        {
            data.Timer -= context.DeltaTime;

            if (data.Timer > 0)
            {
                return ActionRunState.Continue;
            }

            return ActionRunState.Stop;
        }
    }
}