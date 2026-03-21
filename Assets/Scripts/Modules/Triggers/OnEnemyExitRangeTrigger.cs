using System;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    [Serializable]
    public class OnEnemyExitRangeTrigger : TriggerInstance
    {
        [SerializeField] private float range = 10f;
        [SerializeField] private float cooldown = 0.5f;

        [NonSerialized] private int _lastEnemyCount;

        public override void Tick(float deltaTime)
        {
            CooldownTimer -= deltaTime;
            if (CooldownTimer > 0f) return;

            if (Owner?.FirePoint == null) return;

            var colliders = Physics.OverlapSphere(Owner.FirePoint.position, range);
            int enemyCount = 0;
            foreach (var col in colliders)
            {
                if (col.GetComponentInParent<ITargetable>() is { IsAlive: true })
                    enemyCount++;
            }

            if (enemyCount < _lastEnemyCount && _lastEnemyCount > 0)
            {
                Fire();
                CooldownTimer = cooldown;
            }

            _lastEnemyCount = enemyCount;
        }

        public override TriggerInstance CreateInstance()
        {
            return new OnEnemyExitRangeTrigger { range = range, cooldown = cooldown };
        }
    }
}
