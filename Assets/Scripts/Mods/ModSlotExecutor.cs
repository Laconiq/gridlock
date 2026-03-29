using System.Collections.Generic;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Grid;
using Gridlock.Interfaces;
using Gridlock.Towers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gridlock.Mods
{
    public class ModSlotExecutor : MonoBehaviour
    {
        [SerializeField] private ModSlotPreset preset;
        [SerializeField, Required] private GameObject projectilePrefab;
        [SerializeField] private TargetingMode targetingMode = TargetingMode.First;
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<ModSlotData> modSlots = new();

        [SerializeField] private float minFireRate = 0.1f;
        [SerializeField] private float maxFireRate = 20f;

        private TowerChassis _chassis;
        private float _fireTimer;
        private readonly List<SynergyEffect> _activeSynergies = new();
        private GridManager _gridManager;

        private ProjectileConfig _cachedConfig;
        private bool _configDirty = true;

        public List<ModSlotData> ModSlots => modSlots;
        public TargetingMode TargetingMode
        {
            get => targetingMode;
            set => targetingMode = value;
        }

        private void Start()
        {
            _chassis = GetComponent<TowerChassis>();
            _gridManager = ServiceLocator.Get<GridManager>();

            if (preset != null)
                ApplyPreset(preset);
        }

        private void Update()
        {
            if (_chassis == null || modSlots.Count == 0) return;

            if (_configDirty)
            {
                _cachedConfig = ChainCompiler.Compile(modSlots, _chassis.BaseDamage, _activeSynergies);
                _configDirty = false;
            }

            float fireRate = Mathf.Clamp(_chassis.FireRate, minFireRate, maxFireRate);

            if (_activeSynergies.Contains(SynergyEffect.Machinegun))
                fireRate *= 2f;

            fireRate = Mathf.Min(fireRate, maxFireRate);

            float interval = 1f / fireRate;
            _fireTimer += Time.deltaTime;
            if (_fireTimer < interval) return;

            var target = SelectTarget();
            if (target == null) return;

            _fireTimer -= interval;

            SpawnProjectile(_cachedConfig, target);
        }

        public void ApplyPreset(ModSlotPreset newPreset)
        {
            preset = newPreset;
            targetingMode = newPreset.targetingMode;

            modSlots.Clear();
            foreach (var modType in newPreset.slots)
                modSlots.Add(new ModSlotData { modType = modType });

            _configDirty = true;
        }

        public void SetSlots(List<ModSlotData> slots)
        {
            modSlots.Clear();
            modSlots.AddRange(slots);
            _configDirty = true;
        }

        private ITargetable SelectTarget()
        {
            var entries = EnemyRegistry.All;
            if (entries.Count == 0) return null;

            float rangeSq = _chassis.BaseRange * _chassis.BaseRange;
            Vector3 towerPos = transform.position;

            EnemyEntry best = default;
            float bestScore = float.MaxValue;
            bool found = false;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Controller == null || !entry.Controller.IsAlive) continue;

                float distSq = (entry.Controller.Position - towerPos).sqrMagnitude;
                if (distSq > rangeSq) continue;

                float score = EvaluateTarget(entry, distSq);

                if (score < bestScore)
                {
                    bestScore = score;
                    best = entry;
                    found = true;
                }
            }

            return found ? best.Controller : null;
        }

        private float EvaluateTarget(EnemyEntry entry, float distSqToTower)
        {
            switch (targetingMode)
            {
                case TargetingMode.First:
                    return EvaluateFirst(entry);

                case TargetingMode.Last:
                    return -EvaluateFirst(entry);

                case TargetingMode.Nearest:
                    return distSqToTower;

                case TargetingMode.Strongest:
                    return entry.Health != null ? -entry.Health.CurrentHP : 0f;

                case TargetingMode.Weakest:
                    return entry.Health != null ? entry.Health.CurrentHP : float.MaxValue;

                default:
                    return distSqToTower;
            }
        }

        private float EvaluateFirst(EnemyEntry entry)
        {
            return -entry.Controller.RouteIndex;
        }

        [SerializeField] private float splitArcDegrees = 30f;

        private void SpawnProjectile(ProjectileConfig config, ITargetable target)
        {
            if (projectilePrefab == null) return;

            Transform fp = _chassis.FirePoint != null ? _chassis.FirePoint : transform;
            Vector3 spawnPos = fp.position;

            var singleConfig = config.DeepCopy();
            singleConfig.split = false;

            if (config.split)
            {
                int count = config.splitCount;
                if (_activeSynergies.Contains(SynergyEffect.Barrage))
                    count = 5;

                Vector3 baseDir = (target.Position - spawnPos).normalized;
                if (baseDir.sqrMagnitude < 0.001f) baseDir = fp.forward;

                float startAngle = -splitArcDegrees / 2f;
                float step = count > 1 ? splitArcDegrees / (count - 1) : 0f;

                for (int i = 0; i < count; i++)
                {
                    float angle = startAngle + step * i;
                    var dir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

                    var go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
                    var proj = go.GetComponent<ModProjectile>();
                    if (proj != null)
                    {
                        proj.Initialize(singleConfig, target, spawnPos, _activeSynergies);
                        proj.OverrideDirection(dir);
                    }
                }
            }
            else
            {
                var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                var proj = go.GetComponent<ModProjectile>();
                if (proj != null)
                    proj.Initialize(singleConfig, target, spawnPos, _activeSynergies);
            }
        }
    }
}
