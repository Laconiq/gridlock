using AIWE.Enemies;
using AIWE.LevelDesign;
using AIWE.Loot;
using AIWE.Network;
using AIWE.Player;
using AIWE.Towers;
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

            if (IsServer && CurrentState.Value == GameState.Lobby)
                SetState(GameState.Preparing);
        }

        public override void OnNetworkDespawn()
        {
            CurrentState.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState current)
        {
            Debug.Log($"[GameManager] State changed: {previous} -> {current}");

            if (IsServer && current == GameState.Preparing)
                RespawnDeadPlayers();
        }

        public void SetState(GameState newState)
        {
            if (!IsServer) return;
            CurrentState.Value = newState;
        }

        public void CheckTotalPartyKill()
        {
            if (!IsServer || CurrentState.Value != GameState.Wave) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var health = client.PlayerObject?.GetComponent<PlayerHealth>();
                if (health != null && health.IsAlive) return;
            }

            SetState(GameState.GameOver);
        }

        [Rpc(SendTo.Server)]
        public void RequestResetGameServerRpc()
        {
            ResetGame();
        }

        public void ResetGame()
        {
            if (!IsServer) return;

            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                if (enemy.NetworkObject != null && enemy.NetworkObject.IsSpawned)
                    enemy.NetworkObject.Despawn(true);
            }

            foreach (var pickup in FindObjectsByType<ModulePickup>(FindObjectsInactive.Exclude))
            {
                if (pickup.NetworkObject != null && pickup.NetworkObject.IsSpawned)
                    pickup.NetworkObject.Despawn(true);
            }

            foreach (var tower in FindObjectsByType<TowerChassis>(FindObjectsInactive.Exclude))
            {
                if (tower.NetworkObject != null && tower.NetworkObject.IsSpawned)
                    tower.NetworkObject.Despawn(true);
            }

            var linker = FindAnyObjectByType<LDtkLevelLinker>();
            if (linker != null) linker.ResetBuiltState();

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;

                var inventory = client.PlayerObject.GetComponent<PlayerInventory>();
                if (inventory != null) inventory.ResetToDefault();

                var weapon = client.PlayerObject.GetComponent<PlayerWeaponChassis>();
                if (weapon != null) weapon.ResetToDefault();
            }

            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null) lockManager.ForceReleaseLock();

            var wm = FindAnyObjectByType<WaveManager>();
            wm?.ResetWaves();

            ObjectiveController.Instance?.ResetHP();

            SetState(GameState.Preparing);
        }

        private void RespawnDeadPlayers()
        {
            if (!IsServer) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var health = client.PlayerObject?.GetComponent<PlayerHealth>();
                if (health != null && !health.IsAlive)
                    health.Respawn();
            }
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
