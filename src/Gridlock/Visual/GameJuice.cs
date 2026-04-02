using System;
using System.Numerics;
using Gridlock.Grid;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class GameJuice
    {
        public static GameJuice? Instance { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        public float BloomIntensity { get; private set; } = 1.5f;
        public float ChromaticIntensity { get; private set; }

        private Vector3 _shakeOffset;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeIntensity;
        private float _freezeTimer;
        private float _savedTimeScale;

        public void Init() { Instance = this; }
        public void Shutdown() { Instance = null; }

        public void OnEnemyHit(Vector3 pos)
        {
            Shake(0.05f, 0.08f);
            GridWarpManager.Instance?.DropStone(pos, 3f, 3f, new Color(255, 102, 26, 255));
        }

        public void OnEnemyKilled(Vector3 pos)
        {
            Shake(0.12f, 0.25f);
            FreezeFrame(0.04f);
            ChromaticIntensity = 0.6f;
            BloomIntensity = 3.5f;
            GridWarpManager.Instance?.Shockwave(pos, 5.5f, 6f, new Color(255, 38, 38, 255));
        }

        public void OnTowerFired(Vector3 pos)
        {
            GridWarpManager.Instance?.DropStone(pos, 1.5f, 2f, new Color(0, 255, 255, 255));
        }

        public void OnTowerPlaced(Vector3 pos)
        {
            GridWarpManager.Instance?.Shockwave(pos, 5f, 3f, new Color(0, 255, 179, 255));
        }

        private void Shake(float duration, float intensity)
        {
            if (intensity > _shakeIntensity || _shakeTimer <= 0f)
            {
                _shakeDuration = duration;
                _shakeIntensity = intensity;
                _shakeTimer = duration;
            }
        }

        private void FreezeFrame(float duration)
        {
            if (_freezeTimer <= 0f)
                _savedTimeScale = TimeScale;

            _freezeTimer = duration;
            TimeScale = 0f;
        }

        public void Update(float unscaledDt)
        {
            if (_freezeTimer > 0f)
            {
                _freezeTimer -= unscaledDt;
                if (_freezeTimer <= 0f)
                {
                    TimeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
                    _freezeTimer = 0f;
                }
            }

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= unscaledDt;
                float t = Math.Clamp(_shakeTimer / _shakeDuration, 0f, 1f);
                float strength = _shakeIntensity * t * t;
                _shakeOffset = new Vector3(
                    (Random.Shared.NextSingle() * 2f - 1f) * strength,
                    (Random.Shared.NextSingle() * 2f - 1f) * strength,
                    0f
                );
                if (_shakeTimer <= 0f)
                {
                    _shakeOffset = Vector3.Zero;
                    _shakeIntensity = 0f;
                }
            }

            BloomIntensity += (1.5f - BloomIntensity) * Math.Min(6f * unscaledDt, 1f);
            ChromaticIntensity += (0f - ChromaticIntensity) * Math.Min(4f * unscaledDt, 1f);
            if (ChromaticIntensity < 0.001f) ChromaticIntensity = 0f;
        }

        public Vector3 GetShakeOffset() => _shakeOffset;
    }
}
