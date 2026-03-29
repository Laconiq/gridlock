using System;
using System.Collections.Generic;
using Gridlock.Modules;
using UnityEngine;

namespace Gridlock.Player
{
    [Serializable]
    public struct ModuleSlot : IEquatable<ModuleSlot>
    {
        public string ModuleId;
        public int Count;

        public bool Equals(ModuleSlot other)
        {
            return ModuleId == other.ModuleId && Count == other.Count;
        }

        public override int GetHashCode() => System.HashCode.Combine(ModuleId, Count);
    }

    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private DefaultLoadout defaultLoadout;
        [SerializeField] private ModuleRegistry moduleRegistry;

        private readonly List<ModuleSlot> _modules = new();

        public event Action OnInventoryChanged;

        private void Start()
        {
            if (defaultLoadout != null)
                ApplyLoadout(defaultLoadout);
        }

        public void ResetToDefault()
        {
            if (defaultLoadout == null) return;
            ApplyLoadout(defaultLoadout);
        }

        private void ApplyLoadout(DefaultLoadout loadout)
        {
            _modules.Clear();
            int total = 0;
            foreach (var entry in loadout.entries)
            {
                if (entry.module == null || entry.count <= 0) continue;
                _modules.Add(new ModuleSlot
                {
                    ModuleId = entry.module.moduleId,
                    Count = entry.count
                });
                total += entry.count;
            }
            Debug.Log($"[PlayerInventory] Loadout applied: {total} modules ({_modules.Count} types)");
            OnInventoryChanged?.Invoke();
        }

        public bool HasModule(string moduleId)
        {
            return GetCount(moduleId) > 0;
        }

        public int GetCount(string moduleId)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].ModuleId == moduleId)
                    return _modules[i].Count;
            }
            return 0;
        }

        public List<ModuleSlot> GetAllModules()
        {
            return new List<ModuleSlot>(_modules);
        }

        public void AddModule(string moduleId, int count = 1)
        {
            if (count <= 0) return;
            AddModuleInternal(moduleId, count);
            OnInventoryChanged?.Invoke();
        }

        public void RemoveModule(string moduleId, int count = 1)
        {
            if (count <= 0) return;
            RemoveModuleInternal(moduleId, count);
            OnInventoryChanged?.Invoke();
        }

        private void AddModuleInternal(string moduleId, int count)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].ModuleId == moduleId)
                {
                    _modules[i] = new ModuleSlot
                    {
                        ModuleId = moduleId,
                        Count = _modules[i].Count + count
                    };
                    return;
                }
            }
            _modules.Add(new ModuleSlot { ModuleId = moduleId, Count = count });
        }

        private void RemoveModuleInternal(string moduleId, int count)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].ModuleId == moduleId)
                {
                    int newCount = Mathf.Max(0, _modules[i].Count - count);
                    _modules[i] = new ModuleSlot
                    {
                        ModuleId = moduleId,
                        Count = newCount
                    };
                    return;
                }
            }
        }
    }
}
