using System;
using AIWE.Combat;
using AIWE.Interfaces;
using AIWE.LevelDesign;
using UnityEngine;

namespace AIWE.Core
{
    public class ObjectiveController : MonoBehaviour, IDamageable
    {
        public static ObjectiveController Instance { get; private set; }

        private float _currentHP;
        private float _maxHP;

        public float CurrentHP => _currentHP;
        public float MaxHP => _maxHP;
        public float HPNormalized => _maxHP > 0f ? _currentHP / _maxHP : 0f;
        public bool IsAlive => _currentHP > 0f;

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

        private void Start()
        {
            SetHP(_maxHP);
            Debug.Log($"[ObjectiveController] Initialized. HP: {_currentHP}/{_maxHP}");
        }

        private void SetHP(float value)
        {
            float previous = _currentHP;
            _currentHP = value;
            OnHPChanged?.Invoke(_currentHP, _maxHP);

            if (previous > 0f && _currentHP <= 0f)
                OnDestroyed?.Invoke();
        }

        public void ResetHP()
        {
            SetHP(_maxHP);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;

            float newHP = Mathf.Max(0f, _currentHP - damage.Amount);
            SetHP(newHP);

            if (newHP <= 0f)
                GameManager.Instance?.SetState(GameState.GameOver);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<ObjectiveController>();
                Instance = null;
            }
        }
    }
}
