using AIWE.Network;
using AIWE.Player;
using UnityEngine.UIElements;

namespace AIWE.HUD
{
    public class HUDPlayerStatus
    {
        private const string LowClass = "hud__hp-bar-fill--low";
        private const string CriticalClass = "hud__hp-bar-fill--critical";

        private readonly Label _selfName;
        private readonly VisualElement _hpFill;
        private readonly Label _selfSignal;
        private readonly Label _selfHpPct;

        private PlayerHealth _health;
        private PlayerData _data;
        private bool _dirty;

        public HUDPlayerStatus(Label selfName, VisualElement hpFill, Label selfSignal, Label selfHpPct)
        {
            _selfName = selfName;
            _hpFill = hpFill;
            _selfSignal = selfSignal;
            _selfHpPct = selfHpPct;
        }

        public void Bind(PlayerHealth health, PlayerData data)
        {
            _health = health;
            _data = data;

            _health.OnHPChanged += HandleHPChanged;
            _data.OnDataChanged += HandleDataChanged;

            _dirty = true;
        }

        public void Unbind()
        {
            if (_health != null) _health.OnHPChanged -= HandleHPChanged;
            if (_data != null) _data.OnDataChanged -= HandleDataChanged;

            _health = null;
            _data = null;
        }

        public void Refresh()
        {
            if (_health == null || !_dirty) return;
            _dirty = false;

            float normalized = _health.HPNormalized;
            int pct = (int)(normalized * 100f);

            if (_hpFill != null)
                _hpFill.style.width = Length.Percent(normalized * 100f);

            if (_selfHpPct != null)
                _selfHpPct.text = $"{pct}%";

            if (_selfName != null && _data != null)
                _selfName.text = _data.DisplayName;

            if (_selfSignal != null)
                _selfSignal.text = _health.IsAlive ? "NOMINAL" : "OFFLINE";

            UpdateHPClasses(normalized);
        }

        private void UpdateHPClasses(float normalized)
        {
            if (_hpFill == null) return;

            if (normalized < 0.15f)
            {
                _hpFill.AddToClassList(CriticalClass);
                _hpFill.AddToClassList(LowClass);
            }
            else if (normalized < 0.30f)
            {
                _hpFill.RemoveFromClassList(CriticalClass);
                _hpFill.AddToClassList(LowClass);
            }
            else
            {
                _hpFill.RemoveFromClassList(CriticalClass);
                _hpFill.RemoveFromClassList(LowClass);
            }
        }

        private void HandleHPChanged(float current, float max)
        {
            _dirty = true;
        }

        private void HandleDataChanged()
        {
            _dirty = true;
        }
    }
}
