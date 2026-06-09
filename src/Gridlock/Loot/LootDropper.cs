using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Mods;

namespace Gridlock.Loot
{
    public sealed class LootDropper
    {
        public static LootDropper? Instance { get; private set; }

        private readonly LootTable _lootTable;
        private readonly List<ModulePickup> _activePickups = new();

        private static readonly Random _rng = new();

        public IReadOnlyList<ModulePickup> ActivePickups => _activePickups;

        public LootDropper(LootTable lootTable)
        {
            _lootTable = lootTable;
            Instance = this;
        }

        public void OnEnemyDied(Vector3 position)
        {
            if (_rng.NextDouble() > _lootTable.DropChance) return;

            var modType = _lootTable.Roll();
            var pickup = new ModulePickup(modType, position);
            _activePickups.Add(pickup);
        }

        public void Update(float dt, Vector3 collectTarget)
        {
            for (int i = 0; i < _activePickups.Count; i++)
                _activePickups[i].Update(dt, collectTarget);

            for (int i = _activePickups.Count - 1; i >= 0; i--)
            {
                if (!_activePickups[i].Collected && !_activePickups[i].Expired) continue;

                int last = _activePickups.Count - 1;
                _activePickups[i] = _activePickups[last];
                _activePickups.RemoveAt(last);
            }
        }

        public void Clear()
        {
            _activePickups.Clear();
        }

        public void Shutdown()
        {
            if (Instance == this) Instance = null;
        }
    }
}
