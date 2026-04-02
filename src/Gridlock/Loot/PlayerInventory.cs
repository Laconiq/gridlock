using System;
using System.Collections.Generic;
using Gridlock.Core;
using Gridlock.Mods;
using Gridlock.Towers;

namespace Gridlock.Loot
{
    public sealed class PlayerInventory
    {
        public static PlayerInventory? Instance { get; private set; }

        private readonly Dictionary<ModType, int> _owned = new();
        private IReadOnlyList<Tower>? _towers;

        public event Action<ModType, int>? OnModChanged;

        public IReadOnlyDictionary<ModType, int> All => _owned;

        public void Init()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        public void SetTowerSource(IReadOnlyList<Tower> towers)
        {
            _towers = towers;
        }

        public void AddMod(ModType type, int count = 1)
        {
            _owned.TryGetValue(type, out int current);
            _owned[type] = current + count;
            OnModChanged?.Invoke(type, _owned[type]);
        }

        public void RemoveMod(ModType type, int count = 1)
        {
            if (!_owned.TryGetValue(type, out int current)) return;
            int newCount = Math.Max(0, current - count);
            _owned[type] = newCount;
            OnModChanged?.Invoke(type, newCount);
        }

        public int GetOwned(ModType type)
        {
            return _owned.TryGetValue(type, out int count) ? count : 0;
        }

        public int GetAllocated(ModType type, Tower? excludeTower = null,
            IReadOnlyList<ModSlotData>? overrideSlots = null)
        {
            int used = 0;
            if (_towers != null)
            {
                for (int i = 0; i < _towers.Count; i++)
                {
                    var t = _towers[i];
                    if (t == excludeTower) continue;
                    foreach (var s in t.Executor.ModSlots)
                        if (s.modType == type) used++;
                }
            }

            if (overrideSlots != null)
            {
                for (int i = 0; i < overrideSlots.Count; i++)
                    if (overrideSlots[i].modType == type) used++;
            }

            return used;
        }

        public int GetAvailable(ModType type, Tower? excludeTower = null,
            IReadOnlyList<ModSlotData>? overrideSlots = null)
        {
            return Math.Max(0, GetOwned(type) - GetAllocated(type, excludeTower, overrideSlots));
        }

        public void Shutdown()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<PlayerInventory>();
                Instance = null;
            }
        }
    }
}
