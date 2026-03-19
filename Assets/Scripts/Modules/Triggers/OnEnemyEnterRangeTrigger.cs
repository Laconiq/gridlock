using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    public class OnEnemyEnterRangeTrigger : TriggerInstance
    {
        private readonly float _range;
        private readonly float _cooldown;
        private int _lastEnemyCount;

        public OnEnemyEnterRangeTrigger(float range, float cooldown)
        {
            _range = range;
            _cooldown = cooldown;
        }

        public override void Tick(float deltaTime)
        {
            CooldownTimer -= deltaTime;
            if (CooldownTimer > 0f) return;

            if (Owner?.FirePoint == null) return;

            var colliders = Physics.OverlapSphere(Owner.FirePoint.position, _range);
            int enemyCount = 0;
            foreach (var col in colliders)
            {
                if (col.GetComponentInParent<ITargetable>() is { IsAlive: true })
                {
                    enemyCount++;
                }
            }

            if (enemyCount > _lastEnemyCount && enemyCount > 0)
            {
                Fire();
                CooldownTimer = _cooldown;
            }

            _lastEnemyCount = enemyCount;
        }
    }
}
