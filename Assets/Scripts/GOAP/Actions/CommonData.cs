using CrashKonijn.Agent.Core;

namespace GOAP.Actions
{
    public class CommonData : IActionData
    {
        public ITarget Target { get; set; }
        public float Timer { get; set; }
    }
}