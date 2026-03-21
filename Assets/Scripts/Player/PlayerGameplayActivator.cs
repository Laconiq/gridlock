using System.Collections;
using AIWE.Core;
using AIWE.HUD;
using AIWE.Player.CameraEffects;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerGameplayActivator : NetworkBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private GameObject weaponHolder;
        [SerializeField] private WeaponViewModel weaponViewModel;
        [SerializeField] private WeaponFireFeedback weaponFireFeedback;
        [SerializeField] private HeadBobController headBobController;
        [SerializeField] private SpeedLinesController speedLinesController;
        [SerializeField] private LandingImpactController landingImpactController;
        [SerializeField] private MovementAudio movementAudio;
        [SerializeField] private HitFeedback hitFeedback;

        private DynamicCrosshair _crosshair;
        private GameHUD _gameHUD;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            StartCoroutine(WaitForGameManager());
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentState.OnValueChanged -= OnGameStateChanged;
        }

        private IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
                yield return null;

            GameManager.Instance.CurrentState.OnValueChanged += OnGameStateChanged;
            EvaluateState(GameManager.Instance.CurrentState.Value);
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            EvaluateState(current);
        }

        private void EvaluateState(GameState state)
        {
            bool active = state != GameState.Lobby;
            SetGameplayActive(active);
        }

        private void SetGameplayActive(bool active)
        {
            if (weaponHolder != null)
                weaponHolder.SetActive(active);

            if (weaponViewModel != null)
                weaponViewModel.enabled = active;

            if (weaponFireFeedback != null)
                weaponFireFeedback.enabled = active;

            if (headBobController != null)
                headBobController.enabled = active;

            if (speedLinesController != null)
                speedLinesController.enabled = active;

            if (landingImpactController != null)
                landingImpactController.enabled = active;

            if (movementAudio != null)
                movementAudio.enabled = active;

            if (hitFeedback != null)
                hitFeedback.enabled = active;

            if (_crosshair == null)
                _crosshair = FindAnyObjectByType<DynamicCrosshair>();

            if (_crosshair != null)
                _crosshair.enabled = active;

            if (_gameHUD == null)
                _gameHUD = FindAnyObjectByType<GameHUD>();
            if (_gameHUD != null)
                _gameHUD.SetVisible(active);
        }
    }
}
