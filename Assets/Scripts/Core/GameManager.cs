using System;
using AIWE.Enemies;
using AIWE.Loot;
using AIWE.Towers;
using UnityEngine;

namespace AIWE.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState initialState = GameState.Preparing;

        private GameState _currentState;
        public GameState CurrentState => _currentState;

        public event Action<GameState, GameState> OnStateChanged;

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

        private void Start()
        {
            SetState(initialState);
        }

        public void SetState(GameState newState)
        {
            var previous = _currentState;
            _currentState = newState;
            Debug.Log($"[GameManager] State changed: {previous} -> {newState}");
            OnStateChanged?.Invoke(previous, newState);
        }

        public void RequestResetGame()
        {
            if (_currentState != GameState.GameOver)
            {
                Debug.LogWarning("[GameManager] Reset rejected: game is not in GameOver state");
                return;
            }
            ResetGame();
        }

        public void ResetGame()
        {
            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
                Destroy(enemy.gameObject);

            foreach (var pickup in FindObjectsByType<ModulePickup>(FindObjectsInactive.Exclude))
                Destroy(pickup.gameObject);

            foreach (var tower in FindObjectsByType<TowerChassis>(FindObjectsInactive.Exclude))
                Destroy(tower.gameObject);

            var inventory = FindAnyObjectByType<Player.PlayerInventory>();
            if (inventory != null) inventory.ResetToDefault();

            var wm = FindAnyObjectByType<WaveManager>();
            wm?.ResetWaves();

            ObjectiveController.Instance?.ResetHP();

            SetState(GameState.Preparing);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<GameManager>();
                Instance = null;
            }
        }
    }
}
