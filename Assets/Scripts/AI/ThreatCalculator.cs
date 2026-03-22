using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.AI
{
    public class ThreatCalculator
    {
        private readonly ThreatCalculatorConfig _config;

        public ThreatCalculator(ThreatCalculatorConfig config)
        {
            _config = config;
        }

        public (ITargetable target, float score) Evaluate(Vector3 position, float detectionRadius, Transform enemyTransform)
        {
            var sources = ThreatSource.All;

            ITargetable bestTarget = null;
            float bestScore = 0f;

            foreach (var source in sources)
            {
                float dist = Vector3.Distance(position, source.transform.position);
                if (dist > detectionRadius) continue;

                var targetable = source.GetComponent<ITargetable>();
                if (targetable == null || !targetable.IsAlive) continue;

                float score = CalculateScore(position, detectionRadius, targetable, source, enemyTransform);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = targetable;
                }
            }

            if (bestScore < _config.aggroThreshold)
                return (null, 0f);

            return (bestTarget, bestScore);
        }

        private float CalculateScore(Vector3 enemyPos, float radius, ITargetable target, ThreatSource source, Transform enemyTransform)
        {
            float score = 0f;
            float totalWeight = 0f;

            float dist = Vector3.Distance(enemyPos, target.Position);
            float distanceFactor = 1f - Mathf.Clamp01(dist / radius);
            score += distanceFactor * _config.distanceWeight;
            totalWeight += _config.distanceWeight;

            float losFactor = 0.2f;
            Vector3 eyePos = enemyTransform.position + Vector3.up * 1f;
            Vector3 targetPos = target.Position + Vector3.up * 1f;
            Vector3 direction = targetPos - eyePos;
            if (!Physics.Raycast(eyePos, direction.normalized, out _, direction.magnitude, _config.losObstacleMask, QueryTriggerInteraction.Ignore))
                losFactor = 1f;
            score += losFactor * _config.lineOfSightWeight;
            totalWeight += _config.lineOfSightWeight;

            float dpsFactor = Mathf.Clamp01(source.RecentDPS / _config.maxDPSReference);
            score += dpsFactor * _config.dpsWeight;
            totalWeight += _config.dpsWeight;

            int targetCount = EnemyTargetRegistry.GetTargetCount(target);
            float crowdFactor = 1f - Mathf.Clamp01((float)targetCount / _config.maxCrowdCount);
            score += crowdFactor * _config.crowdWeight;
            totalWeight += _config.crowdWeight;

            return totalWeight > 0f ? score / totalWeight : 0f;
        }
    }
}
