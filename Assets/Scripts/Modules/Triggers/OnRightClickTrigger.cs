using System;

namespace AIWE.Modules.Triggers
{
    [Serializable]
    public class OnRightClickTrigger : TriggerInstance
    {
        public override void Tick(float deltaTime) { }

        public override TriggerInstance CreateInstance()
        {
            return new OnRightClickTrigger();
        }
    }
}
