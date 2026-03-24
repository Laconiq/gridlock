using System;
using System.Collections;
using System.Collections.Generic;
using AIWE.HUD;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Core
{
    public class ReadyManager : NetworkBehaviour
    {
        public static ReadyManager Instance { get; private set; }

        [SerializeField] private float countdownDuration = 3f;

        private readonly NetworkVariable<int> _readyBitmask = new(0);
        private readonly Dictionary<ulong, bool> _readyState = new();
        private bool _countingDown;

        public event Action OnReadyStateChanged;
        public bool IsCountingDown => _countingDown;

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
            if (_countingDown) return;

            var clientId = rpcParams.Receive.SenderClientId;
            _readyState.TryGetValue(clientId, out var wasReady);
            _readyState[clientId] = !wasReady;

            SyncBitmaskFromDictionary();

            if (AreAllPlayersReady())
                StartCoroutine(CountdownAndStartWave());
        }

        private void SyncBitmaskFromDictionary()
        {
            int mask = 0;
            int bit = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (_readyState.TryGetValue(client.ClientId, out var ready) && ready)
                    mask |= 1 << bit;
                bit++;
            }
            _readyBitmask.Value = mask;
        }

        private IEnumerator CountdownAndStartWave()
        {
            _countingDown = true;
            var remaining = Mathf.CeilToInt(countdownDuration);

            for (var i = remaining; i > 0; i--)
            {
                ShowAnnouncementClientRpc($"DEPLOYING_IN::{i}");
                yield return new WaitForSeconds(1f);
            }

            ShowAnnouncementClientRpc("ENGAGE");
            yield return new WaitForSeconds(0.5f);
            HideAnnouncementClientRpc();

            GameManager.Instance.SetState(GameState.Wave);
            _countingDown = false;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ShowAnnouncementClientRpc(string text)
        {
            GameHUD.Instance?.ShowAnnouncement(text);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void HideAnnouncementClientRpc()
        {
            GameHUD.Instance?.HideAnnouncement();
        }

        public bool IsPlayerReady(ulong clientId)
        {
            if (IsServer)
                return _readyState.TryGetValue(clientId, out var ready) && ready;

            var index = GetClientIndex(clientId);
            if (index < 0) return false;
            return (_readyBitmask.Value & (1 << index)) != 0;
        }

        public bool AreAllPlayersReady()
        {
            if (IsServer)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (!_readyState.TryGetValue(client.ClientId, out var ready) || !ready)
                        return false;
                }
                return NetworkManager.Singleton.ConnectedClientsList.Count > 0;
            }

            var count = NetworkManager.Singleton.ConnectedClientsList.Count;
            if (count == 0) return false;
            var expectedMask = (1 << count) - 1;
            return (_readyBitmask.Value & expectedMask) == expectedMask;
        }

        public void ResetAllReady()
        {
            if (!IsServer) return;
            _readyState.Clear();
            _readyBitmask.Value = 0;
        }

        private void OnClientDisconnect(ulong clientId)
        {
            if (!IsServer) return;
            _readyState.Remove(clientId);
            SyncBitmaskFromDictionary();
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
