using System.Collections.Generic;
using Gridlock.Audio;
using Gridlock.Combat;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using Gridlock.Visual;
using UnityEngine;

namespace Gridlock.Mods
{
    public class ModProjectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Homing")]
        [SerializeField] private float homingTurnSpeed = 8f;

        [Header("Shock Chain")]
        [SerializeField] private float shockChainRadius = 3f;
        [SerializeField] private int defaultChainCount = 1;
        [SerializeField] private int teslaChainCount = 3;

        [Header("Burn")]
        [SerializeField] private float burnDamage = 5f;
        [SerializeField] private float burnDuration = 3f;
        [SerializeField] private float burnTickInterval = 0.5f;

        [Header("Frost")]
        [SerializeField] private float frostSlowValue = 0.5f;
        [SerializeField] private float frostDuration = 2f;
        [SerializeField] private float blizzardStunDuration = 0.5f;

        [Header("Leech")]
        [SerializeField] private float leechPercent = 0.2f;
        [SerializeField] private float vampireLeechPercent = 0.4f;

        [Header("Void")]
        [SerializeField] private float voidHPPercent = 0.08f;

        [Header("Low HP Threshold")]
        [SerializeField] private float lowHPThreshold = 0.3f;

        [Header("Split")]
        [SerializeField] private float splitArcDegrees = 120f;

        [Header("Pulse")]
        [SerializeField] private float pulseInterval = 0.3f;

        [Header("Delay")]
        [SerializeField] private float delayDuration = 0.5f;

        [Header("Impact Color")]
        [SerializeField] private Color impactColor = new(0f, 1f, 1f);

        private ProjectileConfig _config;
        private List<SynergyEffect> _synergies;
        private ITargetable _target;
        private Vector3 _direction;
        private float _lifetime;
        private bool _initialized;

        private int _remainingPierces;
        private int _remainingBounces;
        private float _pulseTimer;
        private float _delayTimer;
        private bool _delayActive;
        private bool _delayFired;

        private readonly HashSet<int> _hitInstances = new();
        private readonly HashSet<int> _excludeBuffer = new();

        public void Initialize(ProjectileConfig config, ITargetable target, Vector3 origin, List<SynergyEffect> synergies)
        {
            _config = config;
            _target = target;
            _synergies = synergies != null ? new List<SynergyEffect>(synergies) : new List<SynergyEffect>();
            _remainingPierces = config.pierceCount;
            _remainingBounces = config.bounceCount;
            _lifetime = 0f;
            _pulseTimer = 0f;
            _delayTimer = 0f;
            _delayActive = false;
            _delayFired = false;
            _hitInstances.Clear();

            transform.position = origin;

            if (target != null && target.IsAlive && target.Transform != null)
            {
                _direction = (target.Position - origin).normalized;
            }
            else
            {
                _direction = transform.forward;
            }

            if (_direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(_direction);

            _initialized = true;

            if (GetComponent<WarpFollower>() == null)
                gameObject.AddComponent<WarpFollower>();

            var juice = GameJuice.Instance;
            if (juice != null)
                juice.OnTowerFired(origin);
        }

        public void OverrideDirection(Vector3 dir)
        {
            _direction = dir.normalized;
            if (_direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;
            _lifetime += dt;

            if (_lifetime >= (_config.lifetime > 0f ? _config.lifetime : maxLifetime))
            {
                OnLifetimeExpired();
                return;
            }

            if (_delayActive)
            {
                UpdateDelay(dt);
                return;
            }

            UpdatePulse(dt);
            UpdateHoming(dt);
            Move(dt);
            CheckCollision(dt);
        }

        private void UpdateHoming(float dt)
        {
            if (!_config.homing) return;
            if (_target == null || !_target.IsAlive || _target.Transform == null) return;

            var toTarget = _target.Position - transform.position;
            if (toTarget.sqrMagnitude < 0.001f) return;
            toTarget.Normalize();

            if (HasSynergy(SynergyEffect.Missile))
            {
                _direction = toTarget;
            }
            else
            {
                _direction = Vector3.Lerp(_direction, toTarget, homingTurnSpeed * dt).normalized;
            }

            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void Move(float dt)
        {
            transform.position += _direction * (_config.speed * dt);
        }

        private void CheckCollision(float dt)
        {
            float castDistance = _config.speed * dt + 0.2f;
            var origin = transform.position - _direction * 0.1f;

            if (!Physics.SphereCast(origin, _config.size, _direction, out var hit, castDistance, hitMask))
                return;

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            int instanceId = GetEnemyId(hit.collider);
            if (_hitInstances.Contains(instanceId)) return;
            _hitInstances.Add(instanceId);

            float damage = CalculateDamage(hit.collider);
            bool wasAlive = true;
            var health = hit.collider.GetComponentInParent<EnemyHealth>();
            if (health != null)
                wasAlive = health.IsAlive;

            damageable.TakeDamage(new DamageInfo(damage, DamageType.Projectile));

            ApplyElementalEffects(hit.collider, damage);
            ApplyWideAreaDamage(hit.point, damage);
            ApplyShockChain(hit.collider, damage);
            ApplyLeech(damage);

            FireImpactFeedback(hit.point);
            FireEventStages(ModType.OnHit, hit.point, hit.collider);

            bool killedThisHit = wasAlive && health != null && !health.IsAlive;
            if (killedThisHit)
            {
                FireEventStages(ModType.OnKill, hit.point, hit.collider);

                float overkill = -health.CurrentHP;
                if (overkill > 0f)
                    FireEventStages(ModType.OnOverkill, hit.point, hit.collider);
            }

            FireConditionalEvents(hit.point, hit.collider);

            if (HandleBounce(hit.collider))
            {
                FireEventStages(ModType.OnBounce, hit.point, hit.collider);
                return;
            }

            if (HandlePierce())
            {
                FireEventStages(ModType.OnPierce, hit.point, hit.collider);
                return;
            }

            Destroy(gameObject);
        }

        private static int GetEnemyId(Collider collider)
        {
            var controller = collider.GetComponentInParent<EnemyController>();
            return controller != null ? controller.gameObject.GetInstanceID() : collider.GetInstanceID();
        }

        private float CalculateDamage(Collider target)
        {
            if (_config.isVoid)
            {
                var health = target.GetComponentInParent<EnemyHealth>();
                if (health != null)
                    return health.CurrentHP * voidHPPercent;
            }
            return _config.damage;
        }

        private void ApplyElementalEffects(Collider target, float damage)
        {
            var statusManager = target.GetComponentInParent<StatusEffectManager>();
            if (statusManager == null) return;

            if (_config.burn)
            {
                statusManager.ApplyEffect(new StatusEffectData
                {
                    Type = StatusEffectType.DamageOverTime,
                    Value = burnDamage,
                    Duration = burnDuration,
                    TickInterval = burnTickInterval
                });
            }

            if (_config.frost)
            {
                statusManager.ApplyEffect(new StatusEffectData
                {
                    Type = StatusEffectType.Slow,
                    Value = frostSlowValue,
                    Duration = frostDuration
                });

                if (HasSynergy(SynergyEffect.Blizzard))
                {
                    statusManager.ApplyEffect(new StatusEffectData
                    {
                        Type = StatusEffectType.Slow,
                        Value = 0f,
                        Duration = blizzardStunDuration
                    });
                }
            }
        }

        private void ApplyShockChain(Collider sourceCollider, float damage)
        {
            if (!_config.shock) return;

            int chainCount = HasSynergy(SynergyEffect.Tesla) ? teslaChainCount : defaultChainCount;
            int sourceId = GetEnemyId(sourceCollider);

            _excludeBuffer.Clear();
            _excludeBuffer.Add(sourceId);
            var chainOrigin = sourceCollider.transform.position;

            for (int i = 0; i < chainCount; i++)
            {
                var nearestEntry = FindNearestEnemy(chainOrigin, shockChainRadius, _excludeBuffer);
                if (!nearestEntry.HasValue) break;
                var nearest = nearestEntry.Value;

                _excludeBuffer.Add(nearest.Controller.gameObject.GetInstanceID());
                nearest.Health.TakeDamage(new DamageInfo(damage, DamageType.Projectile));
                ImpactFlash.Spawn(nearest.Controller.Position, new Color(0.5f, 0.5f, 1f));
                FireEventStages(ModType.OnChain, nearest.Controller.Position, null);
                chainOrigin = nearest.Controller.Position;
            }
        }

        private void ApplyWideAreaDamage(Vector3 point, float damage)
        {
            if (!_config.wide) return;

            float radius = _config.wideRadius;
            if (HasSynergy(SynergyEffect.Meteor))
                radius *= 2f;

            float radiusSq = radius * radius;
            var entries = EnemyRegistry.All;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Controller == null || !entry.Controller.IsAlive) continue;

                int id = entry.Controller.gameObject.GetInstanceID();
                if (_hitInstances.Contains(id)) continue;

                float distSq = (entry.Controller.Position - point).sqrMagnitude;
                if (distSq > radiusSq) continue;

                _hitInstances.Add(id);
                entry.Health.TakeDamage(new DamageInfo(damage, DamageType.Projectile));
            }
        }

        private void ApplyLeech(float damage)
        {
            if (!_config.leech) return;

            var objective = ObjectiveController.Instance;
            if (objective == null) return;

            float percent = HasSynergy(SynergyEffect.Vampire) ? vampireLeechPercent : leechPercent;
            objective.Heal(damage * percent);
        }

        private bool HandlePierce()
        {
            if (!_config.pierce) return false;

            _remainingPierces--;
            return _remainingPierces > 0;
        }

        private bool HandleBounce(Collider lastHit)
        {
            if (!_config.bounce) return false;
            if (_remainingBounces <= 0) return false;

            _remainingBounces--;

            int lastId = GetEnemyId(lastHit);
            _excludeBuffer.Clear();
            _excludeBuffer.Add(lastId);
            var nearestEntry = FindNearestEnemy(transform.position, shockChainRadius * 3f, _excludeBuffer);
            if (!nearestEntry.HasValue) return false;
            var nearest = nearestEntry.Value;

            _target = nearest.Controller;
            _direction = (nearest.Controller.Position - transform.position).normalized;
            if (_direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(_direction);

            return true;
        }

        private void FireConditionalEvents(Vector3 point, Collider target)
        {
            if (_config.eventStages == null) return;

            var statusManager = target != null ? target.GetComponentInParent<StatusEffectManager>() : null;
            var health = target != null ? target.GetComponentInParent<EnemyHealth>() : null;

            if (statusManager != null && statusManager.HasEffectOfType(StatusEffectType.DamageOverTime))
                FireEventStages(ModType.IfBurning, point, target);

            if (statusManager != null && statusManager.SpeedMultiplier < 1f)
                FireEventStages(ModType.IfFrozen, point, target);

            if (statusManager != null && statusManager.HasEffectOfType(StatusEffectType.DamageOverTime) && _config.shock)
                FireEventStages(ModType.IfShocked, point, target);

            if (health != null && health.MaxHP > 0f && health.CurrentHP / health.MaxHP <= lowHPThreshold)
                FireEventStages(ModType.IfLow, point, target);
        }

        private void FireEventStages(ModType eventType, Vector3 point, Collider target)
        {
            if (_config.eventStages == null) return;

            foreach (var stage in _config.eventStages)
            {
                if (stage.eventType != eventType) continue;
                SpawnSubProjectile(stage.subProjectile, point, target);
            }
        }

        private void SpawnSubProjectile(ProjectileConfig subConfig, Vector3 origin, Collider target)
        {
            if (subConfig.split)
            {
                SpawnSplitProjectiles(subConfig, origin);
                return;
            }

            ITargetable subTarget = null;
            if (target != null)
                subTarget = target.GetComponentInParent<ITargetable>();

            if (subTarget == null || !subTarget.IsAlive)
            {
                var nearest = FindNearestEnemy(origin, shockChainRadius * 3f, null);
                subTarget = nearest?.Controller;
            }

            var go = Instantiate(gameObject, origin, Quaternion.identity);
            var proj = go.GetComponent<ModProjectile>();
            proj.Initialize(subConfig, subTarget, origin, _synergies);
        }

        private void SpawnSplitProjectiles(ProjectileConfig config, Vector3 origin)
        {
            int count = config.splitCount;
            if (HasSynergy(SynergyEffect.Barrage))
                count = 5;

            float startAngle = -splitArcDegrees / 2f;
            float step = count > 1 ? splitArcDegrees / (count - 1) : 0f;

            Vector3 baseDir = _direction;
            if (baseDir.sqrMagnitude < 0.001f)
                baseDir = Vector3.forward;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                var dir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;

                var go = Instantiate(gameObject, origin, Quaternion.identity);
                var proj = go.GetComponent<ModProjectile>();

                var splitConfig = config;
                splitConfig.split = false;

                ITargetable nearest = FindNearestEnemy(origin, shockChainRadius * 3f, null)?.Controller;
                proj.Initialize(splitConfig, nearest, origin, _synergies);
                proj._direction = dir.normalized;
                if (dir.sqrMagnitude > 0.001f)
                    proj.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private void OnLifetimeExpired()
        {
            FireEventStages(ModType.OnEnd, transform.position, null);
            Destroy(gameObject);
        }

        private void UpdatePulse(float dt)
        {
            if (_config.eventStages == null) return;

            bool hasPulse = false;
            foreach (var stage in _config.eventStages)
            {
                if (stage.eventType == ModType.OnPulse)
                {
                    hasPulse = true;
                    break;
                }
            }
            if (!hasPulse) return;

            _pulseTimer += dt;
            if (_pulseTimer >= pulseInterval)
            {
                _pulseTimer -= pulseInterval;
                FireEventStages(ModType.OnPulse, transform.position, null);
            }
        }

        private void UpdateDelay(float dt)
        {
            _delayTimer += dt;
            if (_delayTimer >= delayDuration && !_delayFired)
            {
                _delayFired = true;
                FireEventStages(ModType.OnDelay, transform.position, null);
                Destroy(gameObject);
            }
        }

        public void StartDelay()
        {
            _delayActive = true;
            _delayTimer = 0f;
            _delayFired = false;
        }

        private void FireImpactFeedback(Vector3 point)
        {
            ImpactFlash.Spawn(point, impactColor);
            SoundManager.Instance?.Play(SoundType.ProjectileImpact, point);

            var juice = GameJuice.Instance;
            if (juice != null)
                juice.OnEnemyHit(point);
        }

        private bool HasSynergy(SynergyEffect effect)
        {
            if (_synergies == null) return false;
            for (int i = 0; i < _synergies.Count; i++)
            {
                if (_synergies[i] == effect) return true;
            }
            return false;
        }

        private EnemyEntry? FindNearestEnemy(Vector3 position, float radius, HashSet<int> excludeIds)
        {
            float radiusSq = radius * radius;
            float bestDistSq = float.MaxValue;
            EnemyEntry? best = null;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Controller == null || !entry.Controller.IsAlive) continue;

                int id = entry.Controller.gameObject.GetInstanceID();
                if (excludeIds != null && excludeIds.Contains(id)) continue;

                float distSq = (entry.Controller.Position - position).sqrMagnitude;
                if (distSq < radiusSq && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = entry;
                }
            }

            return best;
        }
    }
}
