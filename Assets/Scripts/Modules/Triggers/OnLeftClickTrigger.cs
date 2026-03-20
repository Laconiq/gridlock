using System;

namespace AIWE.Modules.Triggers
{
    [Serializable]
    public class OnLeftClickTrigger : TriggerInstance
    {
        public override void Tick(float deltaTime) { }

        public override TriggerInstance CreateInstance()
        {
            return new OnLeftClickTrigger();
        }
    }
}
