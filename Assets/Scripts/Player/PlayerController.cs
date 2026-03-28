using System.Collections;
using Gridlock.Core;
using UnityEngine;

namespace Gridlock.Player
{
    public class PlayerController : MonoBehaviour
    {
        public bool InputEnabled { get; set; } = true;

        private PlayerInputProvider _inputProvider;
        private PlayerInteraction _cachedInteraction;

        private void Awake()
        {
            _inputProvider = GetComponent<PlayerInputProvider>();
            _cachedInteraction = GetComponent<PlayerInteraction>();
        }

        private void Start()
        {
            SetPlayerInputActive(false);
            StartCoroutine(WaitForGameManager());
        }

        private IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
                yield return null;

            GameManager.Instance.OnStateChanged += OnGameStateChanged;
            CheckGameState(GameManager.Instance.CurrentState);
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            CheckGameState(current);
        }

        private void CheckGameState(GameState state)
        {
            bool gameActive = state == GameState.Preparing || state == GameState.Wave;
            SetPlayerInputActive(gameActive);
        }

        public void SetPlayerInputActive(bool active)
        {
            InputEnabled = active;
            _inputProvider?.SetPlayerMapEnabled(active);

            if (_cachedInteraction != null) _cachedInteraction.InputEnabled = active;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
}
