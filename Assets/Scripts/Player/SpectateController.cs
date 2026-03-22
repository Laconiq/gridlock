using System.Collections.Generic;
using AIWE.HUD;
using AIWE.Network;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    public class SpectateController : NetworkBehaviour
    {
        [SerializeField] private int _spectateCameraPriority = 200;

        private GameObject _spectateCameraGO;
        private CinemachineCamera _spectateCamera;
        private readonly List<PlayerHealth> _spectateTargets = new();
        private int _currentTargetIndex;
        private bool _isSpectating;
        private PlayerCamera _playerCamera;
        private CinemachineCamera _ownCamera;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _playerCamera = GetComponentInChildren<PlayerCamera>();
            if (_playerCamera != null)
                _ownCamera = _playerCamera.GetComponentInChildren<CinemachineCamera>();
        }

        public void EnterSpectate()
        {
            if (_isSpectating) return;
            _isSpectating = true;

            RefreshTargets();

            if (_spectateTargets.Count > 0)
            {
                _currentTargetIndex = 0;
                CreateSpectateCamera(_spectateTargets[_currentTargetIndex]);
                ShowOverlayForCurrentTarget();
            }
            else
            {
                GameHUD.Instance?.ShowSpectateOverlay("NO TARGETS");
            }

            if (_ownCamera != null)
                _ownCamera.Priority.Value = 0;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ExitSpectate()
        {
            if (!_isSpectating) return;
            _isSpectating = false;

            DestroySpectateCamera();

            if (_ownCamera != null)
                _ownCamera.Priority.Value = 100;

            GameHUD.Instance?.HideSpectateOverlay();
            _spectateTargets.Clear();
        }

        private void Update()
        {
            if (!_isSpectating) return;

            if (_spectateTargets.Count > 0 && !_spectateTargets[_currentTargetIndex].IsAlive)
                CycleToNextAlive();

            var mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.1f) NextTarget();
            else if (scroll < -0.1f) PrevTarget();
        }

        private void NextTarget()
        {
            if (_spectateTargets.Count == 0) return;

            for (int i = 1; i <= _spectateTargets.Count; i++)
            {
                int idx = (_currentTargetIndex + i) % _spectateTargets.Count;
                if (_spectateTargets[idx].IsAlive)
                {
                    SetTarget(idx);
                    return;
                }
            }
        }

        private void PrevTarget()
        {
            if (_spectateTargets.Count == 0) return;

            for (int i = 1; i <= _spectateTargets.Count; i++)
            {
                int idx = (_currentTargetIndex - i + _spectateTargets.Count) % _spectateTargets.Count;
                if (_spectateTargets[idx].IsAlive)
                {
                    SetTarget(idx);
                    return;
                }
            }
        }

        private void CycleToNextAlive()
        {
            for (int i = 0; i < _spectateTargets.Count; i++)
            {
                int idx = (_currentTargetIndex + i) % _spectateTargets.Count;
                if (_spectateTargets[idx].IsAlive)
                {
                    SetTarget(idx);
                    return;
                }
            }

            DestroySpectateCamera();
            GameHUD.Instance?.UpdateSpectateTarget("NO TARGETS");
        }

        private void SetTarget(int index)
        {
            _currentTargetIndex = index;
            var target = _spectateTargets[index];

            DestroySpectateCamera();
            CreateSpectateCamera(target);
            ShowOverlayForCurrentTarget();
        }

        private void CreateSpectateCamera(PlayerHealth target)
        {
            var targetCameraRig = FindCameraRig(target.transform);
            if (targetCameraRig == null) return;

            var cinemachineCam = targetCameraRig.GetComponentInChildren<CinemachineCamera>();
            Transform parent = cinemachineCam != null ? cinemachineCam.transform : targetCameraRig;

            _spectateCameraGO = new GameObject("SpectateCamera");
            _spectateCameraGO.transform.SetParent(parent, false);
            _spectateCameraGO.transform.localPosition = Vector3.zero;
            _spectateCameraGO.transform.localRotation = Quaternion.identity;

            _spectateCamera = _spectateCameraGO.AddComponent<CinemachineCamera>();
            _spectateCamera.Priority.Value = _spectateCameraPriority;
        }

        private void DestroySpectateCamera()
        {
            if (_spectateCameraGO != null)
            {
                Destroy(_spectateCameraGO);
                _spectateCameraGO = null;
                _spectateCamera = null;
            }
        }

        private void ShowOverlayForCurrentTarget()
        {
            if (_currentTargetIndex < 0 || _currentTargetIndex >= _spectateTargets.Count) return;

            var target = _spectateTargets[_currentTargetIndex];
            var data = target.GetComponent<PlayerData>();
            string name = data != null ? data.DisplayName : target.gameObject.name;

            GameHUD.Instance?.ShowSpectateOverlay(name);
        }

        private void RefreshTargets()
        {
            _spectateTargets.Clear();

            if (NetworkManager.Singleton == null) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                if (client.PlayerObject == NetworkObject) continue;

                var health = client.PlayerObject.GetComponent<PlayerHealth>();
                if (health != null && health.IsAlive)
                    _spectateTargets.Add(health);
            }
        }

        private Transform FindCameraRig(Transform root)
        {
            var rig = root.Find("CameraRig");
            if (rig != null) return rig;

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name.Contains("Camera"))
                    return child;
            }

            return root;
        }
    }
}
