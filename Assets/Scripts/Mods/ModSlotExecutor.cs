using System.Collections.Generic;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Grid;
using Gridlock.Interfaces;
using Gridlock.Mods.Pipeline;
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

        private ModPipeline _cachedPipeline;
        private ModContext _cachedBaseCtx;
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
                (_cachedPipeline, _cachedBaseCtx) = PipelineCompiler.Compile(modSlots, _chassis.BaseDamage, _activeSynergies);
                _configDirty = false;
            }

            float fireRate = Mathf.Clamp(_chassis.FireRate, minFireRate, maxFireRate);

            if (_activeSynergies.Contains(SynergyEffect.Machinegun))
                fireRate = Mathf.Min(fireRate * 2f, maxFireRate);

            float interval = 1f / fireRate;
            _fireTimer += Time.deltaTime;
            if (_fireTimer < interval) return;

            var target = SelectTarget();
            if (target == null) return;

            _fireTimer = 0f;
            SpawnProjectile(target);
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
            return targetingMode switch
            {
                TargetingMode.First => -entry.Controller.RouteIndex,
                TargetingMode.Last => entry.Controller.RouteIndex,
                TargetingMode.Nearest => distSqToTower,
                TargetingMode.Strongest => entry.Health != null ? -entry.Health.CurrentHP : 0f,
                TargetingMode.Weakest => entry.Health != null ? entry.Health.CurrentHP : float.MaxValue,
                _ => distSqToTower
            };
        }

        private void SpawnProjectile(ITargetable target)
        {
            if (projectilePrefab == null) return;

            Transform fp = _chassis.FirePoint != null ? _chassis.FirePoint : transform;
            Vector3 spawnPos = fp.position;
            spawnPos.y = 0.5f;

            var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<ModProjectile>();
            if (proj == null) return;

            var pipeline = _cachedPipeline.Clone();
            var ctx = _cachedBaseCtx.Clone();
            proj.Initialize(pipeline, ctx, target, spawnPos);
        }
    }
}
