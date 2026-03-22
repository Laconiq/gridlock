using System;
using AIWE.Combat;
using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Network;
using AIWE.Player.CameraEffects;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerHealth : NetworkBehaviour, IDamageable
    {
        [SerializeField] private float _maxHP = 100f;

        private readonly NetworkVariable<float> _currentHP = new();

        public float CurrentHP => _currentHP.Value;
        public float MaxHP => _maxHP;
        public float HPNormalized => _maxHP > 0f ? _currentHP.Value / _maxHP : 0f;
        public bool IsAlive => _currentHP.Value > 0f;

        public event Action<float, float> OnHPChanged;
        public event Action OnDeath;
        public event Action OnRespawn;

        public override void OnNetworkSpawn()
        {
            if (IsServer) _currentHP.Value = _maxHP;
            _currentHP.OnValueChanged += HandleHPChanged;
        }

        public override void OnNetworkDespawn()
        {
            _currentHP.OnValueChanged -= HandleHPChanged;
        }

        private void HandleHPChanged(float previous, float current)
        {
            OnHPChanged?.Invoke(current, _maxHP);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsServer || !IsAlive) return;
            _currentHP.Value = Mathf.Max(0f, _currentHP.Value - damage.Amount);
            if (_currentHP.Value <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (!IsServer || !IsAlive) return;
            _currentHP.Value = Mathf.Min(_maxHP, _currentHP.Value + amount);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            DieClientRpc();
            GameManager.Instance?.CheckTotalPartyKill();
            Debug.Log($"[Player] {gameObject.name} died");
        }

        [Rpc(SendTo.Everyone)]
        private void DieClientRpc()
        {
            SetVisuals(false);

            if (IsOwner)
            {
                var spectate = GetComponent<SpectateController>();
                if (spectate != null) spectate.EnterSpectate();
            }
        }

        private PlayerSpawnManager _spawnManager;

        public void Respawn()
        {
            if (!IsServer) return;

            _currentHP.Value = _maxHP;

            if (_spawnManager == null)
                _spawnManager = FindAnyObjectByType<PlayerSpawnManager>();
            var spawnPos = _spawnManager != null
                ? _spawnManager.GetNextSpawnPosition()
                : Vector3.up;

            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = spawnPos;
            if (cc != null) cc.enabled = true;

            RespawnClientRpc();
            OnRespawn?.Invoke();
        }

        [Rpc(SendTo.Everyone)]
        private void RespawnClientRpc()
        {
            SetVisuals(true);

            if (IsOwner)
            {
                var spectate = GetComponent<SpectateController>();
                if (spectate != null) spectate.ExitSpectate();
            }
        }

        private void SetVisuals(bool visible)
        {
            var body = transform.Find("PlayerBody");
            if (body != null) body.gameObject.SetActive(visible);

            var weaponVM = GetComponentInChildren<WeaponViewModel>(true);
            if (weaponVM != null) weaponVM.gameObject.SetActive(visible);
        }
    }
}
