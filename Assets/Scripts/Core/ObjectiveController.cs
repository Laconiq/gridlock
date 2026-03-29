using System;
using Gridlock.Audio;
using Gridlock.Combat;
using Gridlock.Grid;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Core
{
    public class ObjectiveController : MonoBehaviour, IDamageable
    {
        public static ObjectiveController Instance { get; private set; }

        [SerializeField] private float defaultHP = 100f;

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

            var gridManager = ServiceLocator.Get<GridManager>();
            _maxHP = gridManager?.Definition != null ? gridManager.Definition.ObjectiveHP : defaultHP;
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

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            SetHP(Mathf.Min(_currentHP + amount, _maxHP));
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;

            SoundManager.Instance?.Play(SoundType.ObjectiveHit);

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
