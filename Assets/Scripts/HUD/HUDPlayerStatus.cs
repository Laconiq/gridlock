using AIWE.Core;
using UnityEngine.UIElements;

namespace AIWE.HUD
{
    public class HUDPlayerStatus
    {
        private readonly Label _selfName;
        private readonly VisualElement _hpFill;
        private readonly Label _selfSignal;
        private readonly Label _selfHpPct;
        private readonly Label _selfReady;

        private bool _bound;

        public HUDPlayerStatus(Label selfName, VisualElement hpFill, Label selfSignal, Label selfHpPct, Label selfReady)
        {
            _selfName = selfName;
            _hpFill = hpFill;
            _selfSignal = selfSignal;
            _selfHpPct = selfHpPct;
            _selfReady = selfReady;
        }

        public void Bind()
        {
            _bound = true;

            if (_selfName != null) _selfName.text = "OPERATOR_01";
            if (_selfSignal != null) _selfSignal.text = "NOMINAL";
            if (_selfHpPct != null) _selfHpPct.text = "100%";
            if (_hpFill != null) _hpFill.style.width = Length.Percent(100f);
            if (_selfReady != null) _selfReady.text = "";
        }

        public void Unbind()
        {
            _bound = false;
        }

        public void Refresh()
        {
            if (!_bound) return;

            var objective = ObjectiveController.Instance;
            if (objective == null) return;

            float normalized = objective.HPNormalized;
            int pct = (int)(normalized * 100f);

            if (_hpFill != null)
                _hpFill.style.width = Length.Percent(normalized * 100f);

            if (_selfHpPct != null)
                _selfHpPct.text = $"{pct}%";

            if (_selfSignal != null)
                _selfSignal.text = objective.IsAlive ? "NOMINAL" : "OFFLINE";
        }
    }
}
