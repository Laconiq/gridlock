using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public abstract class EffectInstance
    {
        [NonSerialized] public EffectDefinition Definition;
        [NonSerialized] public IChassis Owner;

        protected ulong OwnerSourceId
        {
            get
            {
                if (Owner is MonoBehaviour mb)
                    return (ulong)mb.gameObject.GetInstanceID();
                return 0;
            }
        }

        [SerializeField] protected float cooldown;
        [NonSerialized] private float _cooldownTimer;

        public float Cooldown => cooldown;
        public bool IsReady => _cooldownTimer <= 0f;

        public void TickCooldown(float dt)
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;
        }

        public void StartCooldown()
        {
            _cooldownTimer = cooldown;
        }

        public abstract void Execute(List<ITargetable> targets, Vector3 origin);

        public abstract EffectInstance CreateInstance();
    }
}
