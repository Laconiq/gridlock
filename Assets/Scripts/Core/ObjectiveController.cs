using System;
using AIWE.Combat;
using AIWE.Interfaces;
using AIWE.LevelDesign;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Core
{
    public class ObjectiveController : NetworkBehaviour, IDamageable
    {
        public static ObjectiveController Instance { get; private set; }

        private readonly NetworkVariable<float> _currentHP = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float _maxHP;

        public float CurrentHP => _currentHP.Value;
        public float MaxHP => _maxHP;
        public float HPNormalized => _maxHP > 0f ? _currentHP.Value / _maxHP : 0f;
        public bool IsAlive => _currentHP.Value > 0f;

        public event Action<float, float> OnHPChanged;
        public event Action OnDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            ServiceLocator.Register(this);

            var marker = FindAnyObjectByType<ObjectiveMarker>();
            _maxHP = marker != null ? marker.Health : 100f;
        }

        public override void OnNetworkSpawn()
        {
            _currentHP.OnValueChanged += HandleHPChanged;

            if (IsServer)
                _currentHP.Value = _maxHP;

            Debug.Log($"[ObjectiveController] Spawned. HP: {_currentHP.Value}/{_maxHP}");
        }

        public override void OnNetworkDespawn()
        {
            _currentHP.OnValueChanged -= HandleHPChanged;
        }

        private void HandleHPChanged(float previous, float current)
        {
            OnHPChanged?.Invoke(current, _maxHP);

            if (previous > 0f && current <= 0f)
                OnDestroyed?.Invoke();
        }

        public void ResetHP()
        {
            if (!IsServer) return;
            _currentHP.Value = _maxHP;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsServer) return;
            if (!IsAlive) return;

            float newHP = Mathf.Max(0f, _currentHP.Value - damage.Amount);
            _currentHP.Value = newHP;

            if (newHP <= 0f)
                GameManager.Instance?.SetState(GameState.GameOver);
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<ObjectiveController>();
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
