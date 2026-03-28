using System;
using System.Collections.Generic;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Effects
{
    [Serializable]
    public class HealEffect : EffectInstance
    {
        [SerializeField] private float healAmount = 25f;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            // In top-down solo, heal targets objective HP instead
            var objective = Core.ObjectiveController.Instance;
            if (objective != null && objective.IsAlive)
            {
                objective.ResetHP();
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new HealEffect { healAmount = healAmount, cooldown = cooldown };
        }
    }
}
