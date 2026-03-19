using Unity.Netcode;
using UnityEngine;

namespace AIWE.Core
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        public NetworkVariable<GameState> CurrentState { get; } = new(
            GameState.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

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
            CurrentState.OnValueChanged += OnStateChanged;
            Debug.Log($"[GameManager] Spawned. State: {CurrentState.Value}");
        }

        public override void OnNetworkDespawn()
        {
            CurrentState.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState current)
        {
            Debug.Log($"[GameManager] State changed: {previous} -> {current}");
        }

        public void SetState(GameState newState)
        {
            if (!IsServer) return;
            CurrentState.Value = newState;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<GameManager>();
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
