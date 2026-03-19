using System;
using AIWE.Core;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Network
{
    public class EditorLockManager : NetworkBehaviour
    {
        private readonly NetworkVariable<long> _currentEditorClientId = new(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool IsLocked => _currentEditorClientId.Value >= 0;

        public bool IsLockedByMe =>
            NetworkManager.Singleton != null &&
            _currentEditorClientId.Value == (long)NetworkManager.Singleton.LocalClientId;

        public event Action<bool> OnLockStateChanged;
        public event Action OnLockGranted;
        public event Action OnLockDenied;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public override void OnNetworkSpawn()
        {
            _currentEditorClientId.OnValueChanged += OnLockValueChanged;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
            }
        }

        public override void OnNetworkDespawn()
        {
            _currentEditorClientId.OnValueChanged -= OnLockValueChanged;

            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        private void OnLockValueChanged(long prev, long current)
        {
            OnLockStateChanged?.Invoke(current >= 0);

            if (current == (long)NetworkManager.Singleton.LocalClientId)
            {
                OnLockGranted?.Invoke();
            }
        }

        public bool IsLockedBy(ulong clientId)
        {
            return _currentEditorClientId.Value == (long)clientId;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestLockRpc(ulong clientId)
        {
            if (_currentEditorClientId.Value < 0)
            {
                _currentEditorClientId.Value = (long)clientId;
                Debug.Log($"[EditorLock] Lock granted to client {clientId}");
                GrantLockRpc(clientId);
            }
            else
            {
                Debug.Log($"[EditorLock] Lock denied for client {clientId} (held by {_currentEditorClientId.Value})");
                DenyLockRpc(clientId);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReleaseLockRpc(ulong clientId)
        {
            if (_currentEditorClientId.Value == (long)clientId)
            {
                _currentEditorClientId.Value = -1;
                Debug.Log($"[EditorLock] Lock released by client {clientId}");
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void GrantLockRpc(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                OnLockGranted?.Invoke();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void DenyLockRpc(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                OnLockDenied?.Invoke();
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (IsServer && _currentEditorClientId.Value == (long)clientId)
            {
                _currentEditorClientId.Value = -1;
                Debug.Log($"[EditorLock] Lock auto-released (client {clientId} disconnected)");
            }
        }

        public override void OnDestroy()
        {
            ServiceLocator.Unregister<EditorLockManager>();
            base.OnDestroy();
        }
    }
}
