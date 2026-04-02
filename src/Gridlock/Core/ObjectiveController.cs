using System;
using Gridlock.Combat;
using Gridlock.Grid;

namespace Gridlock.Core
{
    public sealed class ObjectiveController : IDamageable, IObjectiveHealer
    {
        public static ObjectiveController? Instance { get; private set; }

        private float _currentHP;
        private float _maxHP;

        public float CurrentHP => _currentHP;
        public float MaxHP => _maxHP;
        public float HPNormalized => _maxHP > 0f ? _currentHP / _maxHP : 0f;
        public bool IsAlive => _currentHP > 0f;

        public event Action<float, float>? OnHPChanged;
        public event Action? OnDestroyed;

        public ObjectiveController(float defaultHP = 100f)
        {
            _maxHP = defaultHP;
        }

        public void Init(GridManager? gridManager = null)
        {
            Instance = this;
            ServiceLocator.Register(this);

            if (gridManager?.Definition != null)
                _maxHP = gridManager.Definition.ObjectiveHP;

            SetHP(_maxHP);
            Console.WriteLine($"[ObjectiveController] Initialized. HP: {_currentHP}/{_maxHP}");
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
            SetHP(MathF.Min(_currentHP + amount, _maxHP));
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;

            float newHP = MathF.Max(0f, _currentHP - damage.Amount);
            SetHP(newHP);

            if (newHP <= 0f)
                GameManager.Instance?.SetState(GameState.GameOver);
        }

        public void Shutdown()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<ObjectiveController>();
                Instance = null;
            }
        }
    }
}
