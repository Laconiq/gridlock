using System;
using System.Numerics;
using Gridlock.Mods;

namespace Gridlock.Loot
{
    public sealed class ModulePickup
    {
        private const float RotationSpeed = 90f;
        private const float BobAmplitude = 0.15f;
        private const float BobFrequency = 2f;
        private const float Lifetime = 30f;
        private const float MagnetDelay = 1f;
        private const float MagnetSpeed = 15f;
        private const float MagnetAcceleration = 20f;
        private const float ArcHeight = 3f;

        private float _age;
        private readonly float _baseY;
        private readonly Vector3 _spawnPos;

        private bool _magnetActive;
        private float _flightProgress;
        private float _currentSpeed;
        private Vector3 _flightStart;
        private Vector3 _controlPoint;

        public ModType ModType { get; }
        public Rarity Rarity { get; }
        public Vector3 Position { get; private set; }
        public float Rotation { get; private set; }
        public float Scale { get; private set; } = 0.3f;
        public bool Collected { get; private set; }
        public bool Expired => !Collected && _age >= Lifetime;

        public ModulePickup(ModType modType, Vector3 spawnPos)
        {
            ModType = modType;
            Rarity = ModRarity.GetRarity(modType);
            _spawnPos = spawnPos;
            _baseY = spawnPos.Y;
            Position = spawnPos;
        }

        public void Update(float dt, Vector3 collectTarget)
        {
            _age += dt;

            if (_age >= Lifetime)
                return;

            if (!_magnetActive)
            {
                UpdateIdle(dt);
                if (_age >= MagnetDelay)
                    BeginMagnet(collectTarget);
            }
            else
            {
                UpdateMagnet(dt, collectTarget);
            }
        }

        private void UpdateIdle(float dt)
        {
            Rotation += RotationSpeed * dt;
            float y = _baseY + MathF.Sin(_age * BobFrequency * MathF.PI * 2f) * BobAmplitude;
            Position = new Vector3(Position.X, y, Position.Z);
        }

        private void BeginMagnet(Vector3 target)
        {
            _magnetActive = true;
            _flightStart = Position;
            _flightProgress = 0f;
            _currentSpeed = MagnetSpeed * 0.3f;
            _controlPoint = (_flightStart + target) * 0.5f + new Vector3(0f, ArcHeight, 0f);
        }

        private void UpdateMagnet(float dt, Vector3 target)
        {
            _currentSpeed += MagnetAcceleration * dt;
            _flightProgress += _currentSpeed * dt * 0.1f;

            if (_flightProgress >= 1f)
            {
                Collect();
                return;
            }

            float t = _flightProgress;
            float u = 1f - t;
            Position = u * u * _flightStart + 2f * u * t * _controlPoint + t * t * target;
            Scale = 0.3f * (1f - t * t * 0.5f);
        }

        private void Collect()
        {
            Collected = true;
            PlayerInventory.Instance?.AddMod(ModType);
        }
    }
}
