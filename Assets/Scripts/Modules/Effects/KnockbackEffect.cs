using System;
using System.Collections.Generic;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Effects
{
    [Serializable]
    public class KnockbackEffect : EffectInstance
    {
        [SerializeField] private float force = 5f;

        [NonSerialized] private Grid.GridManager _gridManager;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            _gridManager ??= Core.ServiceLocator.Get<Grid.GridManager>();

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var direction = (target.Position - origin).normalized;
                var newPos = target.Transform.position + direction * force;

                if (_gridManager != null && _gridManager.TryWorldToGrid(newPos, out _))
                    target.Transform.position = newPos;
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new KnockbackEffect { force = force, cooldown = cooldown };
        }
    }
}
