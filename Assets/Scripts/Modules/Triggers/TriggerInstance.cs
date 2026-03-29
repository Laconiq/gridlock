using System;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Triggers
{
    [Serializable]
    public abstract class TriggerInstance
    {
        protected static readonly Collider[] SharedOverlapBuffer = new Collider[256];

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
