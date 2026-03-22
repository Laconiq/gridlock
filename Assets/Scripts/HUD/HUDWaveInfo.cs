using AIWE.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.HUD
{
    public class HUDWaveInfo
    {
        private const string LowClass = "hud__objective-bar-fill--low";
        private const string CriticalClass = "hud__objective-bar-fill--critical";

        private readonly Label _waveLabel;
        private readonly Label _enemiesLabel;
        private readonly VisualElement _objectiveHpFill;
        private readonly Label _objectivePct;
        private WaveManager _waveManager;

        public HUDWaveInfo(Label waveLabel, Label enemiesLabel, VisualElement objectiveHpFill, Label objectivePct)
        {
            _waveLabel = waveLabel;
            _enemiesLabel = enemiesLabel;
            _objectiveHpFill = objectiveHpFill;
            _objectivePct = objectivePct;
        }

        public void Refresh()
        {
            RefreshWave();
            RefreshObjective();
        }

        private void RefreshWave()
        {
            if (_waveManager == null)
                _waveManager = Object.FindAnyObjectByType<WaveManager>();
            if (_waveManager == null) return;
            var wm = _waveManager;

            int wave = wm.CurrentWave + 1;

            if (_waveLabel != null)
                _waveLabel.text = $"WAVE {wave:D2}";

            if (_enemiesLabel != null)
                _enemiesLabel.text = $"HOSTILES::{wm.EnemiesRemaining}";
        }

        private void RefreshObjective()
        {
            var obj = ObjectiveController.Instance;
            if (obj == null) return;

            float normalized = obj.HPNormalized;
            int pct = (int)(normalized * 100f);

            if (_objectiveHpFill != null)
                _objectiveHpFill.style.width = Length.Percent(normalized * 100f);

            if (_objectivePct != null)
                _objectivePct.text = $"{pct}%";

            UpdateObjectiveClasses(normalized);
        }

        private void UpdateObjectiveClasses(float normalized)
        {
            if (_objectiveHpFill == null) return;

            if (normalized < 0.15f)
            {
                _objectiveHpFill.AddToClassList(CriticalClass);
                _objectiveHpFill.AddToClassList(LowClass);
            }
            else if (normalized < 0.30f)
            {
                _objectiveHpFill.RemoveFromClassList(CriticalClass);
                _objectiveHpFill.AddToClassList(LowClass);
            }
            else
            {
                _objectiveHpFill.RemoveFromClassList(CriticalClass);
                _objectiveHpFill.RemoveFromClassList(LowClass);
            }
        }
    }
}
