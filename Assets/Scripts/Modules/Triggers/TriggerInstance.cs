using System;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    public abstract class TriggerInstance
    {
        public TriggerDefinition Definition { get; set; }
        public IChassis Owner { get; set; }

        protected float CooldownTimer;

        public event Action OnTriggered;

        public abstract void Tick(float deltaTime);

        protected void Fire()
        {
            OnTriggered?.Invoke();
        }

        public virtual void Reset()
        {
            CooldownTimer = 0f;
        }
    }
}
