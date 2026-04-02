using System.Collections.Generic;

namespace Gridlock.Enemies
{
    public static class EnemyRegistry
    {
        private static readonly List<Enemy> _entries = new(256);

        public static IReadOnlyList<Enemy> All => _entries;
        public static int Count => _entries.Count;

        public static void Register(Enemy enemy)
        {
            _entries.Add(enemy);
        }

        public static void Unregister(Enemy enemy)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i] == enemy)
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
