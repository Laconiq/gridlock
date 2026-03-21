using System;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Core
{
    public class ReadyManager : NetworkBehaviour
    {
        public static ReadyManager Instance { get; private set; }

        private readonly NetworkVariable<int> _readyBitmask = new(0);

        public event Action OnReadyStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ServiceLocator.Register(this);
        }

        public override void OnNetworkSpawn()
        {
            _readyBitmask.OnValueChanged += HandleReadyBitmaskChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
                var gm = GameManager.Instance;
                if (gm != null)
                    gm.CurrentState.OnValueChanged += OnGameStateChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            _readyBitmask.OnValueChanged -= HandleReadyBitmaskChanged;

            if (IsServer)
            {
                if (NetworkManager.Singleton != null)
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

                if (GameManager.Instance != null)
                    GameManager.Instance.CurrentState.OnValueChanged -= OnGameStateChanged;
            }
        }

        private void HandleReadyBitmaskChanged(int previous, int current)
        {
            OnReadyStateChanged?.Invoke();
        }

        [Rpc(SendTo.Server)]
        public void ToggleReadyServerRpc(RpcParams rpcParams = default)
        {
            if (GameManager.Instance?.CurrentState.Value != GameState.Preparing) return;

            var clientId = rpcParams.Receive.SenderClientId;
            var index = GetClientIndex(clientId);
            if (index < 0) return;

            _readyBitmask.Value ^= 1 << index;

            if (AreAllPlayersReady())
                GameManager.Instance.SetState(GameState.Wave);
        }

        public bool IsPlayerReady(ulong clientId)
        {
            var index = GetClientIndex(clientId);
            if (index < 0) return false;
            return (_readyBitmask.Value & (1 << index)) != 0;
        }

        public bool AreAllPlayersReady()
        {
            var count = NetworkManager.Singleton.ConnectedClientsList.Count;
            if (count == 0) return false;
            var expectedMask = (1 << count) - 1;
            return (_readyBitmask.Value & expectedMask) == expectedMask;
        }

        public void ResetAllReady()
        {
            if (!IsServer) return;
            _readyBitmask.Value = 0;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (!IsServer) return;
            _readyBitmask.Value = 0;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Preparing)
                ResetAllReady();
        }

        private int GetClientIndex(ulong clientId)
        {
            var clients = NetworkManager.Singleton.ConnectedClientsList;
            for (var i = 0; i < clients.Count; i++)
            {
                if (clients[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<ReadyManager>();
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
