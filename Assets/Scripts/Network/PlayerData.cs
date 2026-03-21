using System;
using Unity.Collections;
using Unity.Netcode;

namespace AIWE.Network
{
    public class PlayerData : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString64Bytes> _displayName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private readonly NetworkVariable<int> _kills = new();

        public string DisplayName => _displayName.Value.ToString();
        public int Kills => _kills.Value;

        public event Action OnDataChanged;

        public override void OnNetworkSpawn()
        {
            if (IsOwner) _displayName.Value = $"OPERATOR_{OwnerClientId + 1:D2}";

            _displayName.OnValueChanged += HandleDataChanged;
            _kills.OnValueChanged += HandleDataChanged;
        }

        public override void OnNetworkDespawn()
        {
            _displayName.OnValueChanged -= HandleDataChanged;
            _kills.OnValueChanged -= HandleDataChanged;
        }

        private void HandleDataChanged<T>(T previous, T current)
        {
            OnDataChanged?.Invoke();
        }

        public void AddKill()
        {
            if (!IsServer) return;
            _kills.Value++;
        }
    }
}
