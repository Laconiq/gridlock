using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.AI
{
    public static class EnemyTargetRegistry
    {
        private static readonly Dictionary<ITargetable, int> _targetCounts = new();

        public static void RegisterTarget(ITargetable target)
        {
            if (target == null) return;
            _targetCounts.TryGetValue(target, out int count);
            _targetCounts[target] = count + 1;
        }

        public static void UnregisterTarget(ITargetable target)
        {
            if (target == null) return;
            if (_targetCounts.TryGetValue(target, out int count))
            {
                if (count <= 1)
                    _targetCounts.Remove(target);
                else
                    _targetCounts[target] = count - 1;
            }
        }

        public static int GetTargetCount(ITargetable target)
        {
            if (target == null) return 0;
            _targetCounts.TryGetValue(target, out int count);
            return count;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            _targetCounts.Clear();
        }
    }
}
