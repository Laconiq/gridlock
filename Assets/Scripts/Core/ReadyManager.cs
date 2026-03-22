using System;
using System.Collections;
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
            var index = GetClientIndex(clientId);
            if (index < 0) return;

            _readyBitmask.Value ^= 1 << index;

            if (AreAllPlayersReady())
                StartCoroutine(CountdownAndStartWave());
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
