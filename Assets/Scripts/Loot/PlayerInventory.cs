using System;
using System.Collections.Generic;
using System.Linq;
using Gridlock.Core;
using Gridlock.Mods;
using UnityEngine;

namespace Gridlock.Loot
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private int debugStartCount = 5;

        private readonly Dictionary<ModType, int> _inventory = new();

        public event Action<ModType, int> OnModChanged;

        public IReadOnlyDictionary<ModType, int> All => _inventory;

        private void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (debugStartCount > 0)
            {
                foreach (ModType type in Enum.GetValues(typeof(ModType)))
                    _inventory[type] = debugStartCount;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ServiceLocator.Unregister<PlayerInventory>();
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
            int newCount = Mathf.Max(0, current - count);
            _inventory[type] = newCount;
            OnModChanged?.Invoke(type, newCount);
        }

        public int GetCount(ModType type)
        {
            return _inventory.TryGetValue(type, out int count) ? count : 0;
        }
    }
}
