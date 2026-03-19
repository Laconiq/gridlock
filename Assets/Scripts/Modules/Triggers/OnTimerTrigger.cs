using System;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    [Serializable]
    public class OnTimerTrigger : TriggerInstance
    {
        [SerializeField] private float interval = 2f;

        private float _timer;

        public override void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= interval)
            {
                _timer = 0f;
                Fire();
            }
        }

        public override TriggerInstance CreateInstance()
        {
            return new OnTimerTrigger { interval = interval };
        }

        public override void Reset()
        {
            base.Reset();
            _timer = 0f;
        }
    }
}
