using System;
using System.Collections.Generic;
using AIWE.Modules;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    [Serializable]
    public struct ModuleSlot : INetworkSerializable, IEquatable<ModuleSlot>
    {
        public FixedString64Bytes ModuleId;
        public int Count;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ModuleId);
            serializer.SerializeValue(ref Count);
        }

        public bool Equals(ModuleSlot other)
        {
            return ModuleId.Equals(other.ModuleId) && Count == other.Count;
        }

        public override int GetHashCode() => ModuleId.GetHashCode();
    }

    public class PlayerInventory : NetworkBehaviour
    {
        [SerializeField] private DefaultLoadout defaultLoadout;
        [SerializeField] private ModuleRegistry moduleRegistry;

        private NetworkList<ModuleSlot> _modules;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            _modules = new NetworkList<ModuleSlot>();
            _modules.OnListChanged += OnModulesChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer && defaultLoadout != null)
            {
                ApplyLoadout(defaultLoadout);
            }
        }

        public void ResetToDefault()
        {
            if (!IsServer || defaultLoadout == null) return;
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
                    ModuleId = new FixedString64Bytes(entry.module.moduleId),
                    Count = entry.count
                });
                total += entry.count;
            }
            Debug.Log($"[PlayerInventory] Loadout applied: {total} modules ({_modules.Count} types)");
        }

        private void OnModulesChanged(NetworkListEvent<ModuleSlot> changeEvent)
        {
            OnInventoryChanged?.Invoke();
        }

        public bool HasModule(string moduleId)
        {
            return GetCount(moduleId) > 0;
        }

        public int GetCount(string moduleId)
        {
            var fixedId = new FixedString64Bytes(moduleId);
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].ModuleId.Equals(fixedId))
                    return _modules[i].Count;
            }
            return 0;
        }

        public List<ModuleSlot> GetAllModules()
        {
            var result = new List<ModuleSlot>();
            for (int i = 0; i < _modules.Count; i++)
                result.Add(_modules[i]);
            return result;
        }

        public void AddModule(string moduleId, int count = 1)
        {
            if (!IsServer) { AddModuleServerRpc(moduleId, count); return; }
            AddModuleInternal(moduleId, count);
        }

        public void RemoveModule(string moduleId, int count = 1)
        {
            if (!IsServer) { RemoveModuleServerRpc(moduleId, count); return; }
            RemoveModuleInternal(moduleId, count);
        }

        [Rpc(SendTo.Server)]
        private void AddModuleServerRpc(string moduleId, int count)
        {
            if (count <= 0) return;
            if (moduleRegistry != null && moduleRegistry.GetById(moduleId) == null)
            {
                Debug.LogWarning($"[PlayerInventory] Rejected AddModule: unknown moduleId '{moduleId}'");
                return;
            }
            AddModuleInternal(moduleId, count);
        }

        [Rpc(SendTo.Server)]
        private void RemoveModuleServerRpc(string moduleId, int count)
        {
            if (count <= 0) return;
            RemoveModuleInternal(moduleId, count);
        }

        private void AddModuleInternal(string moduleId, int count)
        {
            var fixedId = new FixedString64Bytes(moduleId);
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].ModuleId.Equals(fixedId))
                {
                    _modules[i] = new ModuleSlot
                    {
                        ModuleId = fixedId,
                        Count = _modules[i].Count + count
                    };
                    return;
                }
            }
            _modules.Add(new ModuleSlot { ModuleId = fixedId, Count = count });
        }

        private void RemoveModuleInternal(string moduleId, int count)
        {
            var fixedId = new FixedString64Bytes(moduleId);
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].ModuleId.Equals(fixedId))
                {
                    int newCount = Mathf.Max(0, _modules[i].Count - count);
                    _modules[i] = new ModuleSlot
                    {
                        ModuleId = fixedId,
                        Count = newCount
                    };
                    return;
                }
            }
        }

        public override void OnDestroy()
        {
            _modules.OnListChanged -= OnModulesChanged;
            base.OnDestroy();
        }
    }
}
