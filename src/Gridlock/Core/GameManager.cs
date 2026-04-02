using System;

namespace Gridlock.Core
{
    public sealed class GameManager
    {
        public static GameManager? Instance { get; private set; }

        private GameState _currentState;
        public GameState CurrentState => _currentState;

        public event Action<GameState, GameState>? OnStateChanged;

        public void Init(GameState initialState = GameState.Preparing)
        {
            Instance = this;
            ServiceLocator.Register(this);
            SetState(initialState);
        }

        public void SetState(GameState newState)
        {
            var previous = _currentState;
            _currentState = newState;
            Console.WriteLine($"[GameManager] State changed: {previous} -> {newState}");
            OnStateChanged?.Invoke(previous, newState);
        }

        public void RequestResetGame()
        {
            if (_currentState != GameState.GameOver)
            {
                Console.WriteLine("[GameManager] Reset rejected: game is not in GameOver state");
                return;
            }
            ResetGame();
        }

        public event Action? OnResetGame;

        public void ResetGame()
        {
            GameStats.Instance?.Reset();
            Enemies.EnemyRegistry.Clear();
            OnResetGame?.Invoke();
            SetState(GameState.Preparing);
        }

        public void Shutdown()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<GameManager>();
                Instance = null;
            }
        }
    }
}
