using System;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    [Serializable]
    public abstract class TriggerInstance
    {
        [NonSerialized] public TriggerDefinition Definition;
        [NonSerialized] public IChassis Owner;
        [NonSerialized] protected float CooldownTimer;

        public event Action OnTriggered;

        public abstract void Tick(float deltaTime);

        public abstract TriggerInstance CreateInstance();

        protected void Fire()
        {
            OnTriggered?.Invoke();
        }

        public virtual void ExternalFire() => Fire();

        public virtual void Reset()
        {
            CooldownTimer = 0f;
        }
    }
}
