using System.Collections.Generic;

namespace Gridlock.Enemies
{
    public struct EnemyEntry
    {
        public EnemyController Controller;
        public EnemyHealth Health;
    }

    public static class EnemyRegistry
    {
        private static readonly List<EnemyEntry> _entries = new(256);

        public static IReadOnlyList<EnemyEntry> All => _entries;
        public static int Count => _entries.Count;

        public static void Register(EnemyController controller, EnemyHealth health)
        {
            _entries.Add(new EnemyEntry { Controller = controller, Health = health });
        }

        public static void Unregister(EnemyController controller)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Controller == controller)
                {
                    int last = _entries.Count - 1;
                    if (i < last)
                        _entries[i] = _entries[last];
                    _entries.RemoveAt(last);
                    return;
                }
            }
        }

        public static void Clear()
        {
            _entries.Clear();
        }
    }
}
