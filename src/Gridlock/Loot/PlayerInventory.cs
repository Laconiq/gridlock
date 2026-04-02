using System;
using System.Collections.Generic;
using Gridlock.Core;
using Gridlock.Mods;

namespace Gridlock.Loot
{
    public sealed class PlayerInventory
    {
        public static PlayerInventory? Instance { get; private set; }

        private readonly Dictionary<ModType, int> _inventory = new();

        public event Action<ModType, int>? OnModChanged;

        public IReadOnlyDictionary<ModType, int> All => _inventory;

        public void Init()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        public void AddMod(ModType type, int count = 1)
        {
            _inventory.TryGetValue(type, out int current);
            _inventory[type] = current + count;
            OnModChanged?.Invoke(type, _inventory[type]);
        }

        public void RemoveMod(ModType type, int count = 1)
        {
            if (!_inventory.TryGetValue(type, out int current)) return;
            int newCount = Math.Max(0, current - count);
            _inventory[type] = newCount;
            OnModChanged?.Invoke(type, newCount);
        }

        public int GetCount(ModType type)
        {
            return _inventory.TryGetValue(type, out int count) ? count : 0;
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
