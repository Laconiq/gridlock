using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public float Rotation;
        public float AngularVelocity;
        public byte R, G, B, A;
    }

    public sealed class ParticleEmitter
    {
        public static ParticleEmitter Instance { get; private set; } = new();

        private readonly Particle[] _particles;
        private int _count;

        public ParticleEmitter(int capacity = 512)
        {
            _particles = new Particle[capacity];
            Instance = this;
        }

        public void Burst(Vector3 pos, int count, float speed, float gravity, float lifetime,
            Color color, float coneAngle = 18f)
        {
            float halfAngle = coneAngle * MathF.PI / 180f;

            for (int i = 0; i < count; i++)
            {
                if (_count >= _particles.Length) RecycleOldest();

                float theta = Random.Shared.NextSingle() * MathF.Tau;
                float phi = Random.Shared.NextSingle() * halfAngle;
                float sp = MathF.Sin(phi);

                var dir = new Vector3(
                    MathF.Cos(theta) * sp,
                    MathF.Cos(phi),
                    MathF.Sin(theta) * sp
                );

                float s = speed * (0.5f + Random.Shared.NextSingle());
                float lt = lifetime * (0.6f + Random.Shared.NextSingle() * 0.8f);
                float sz = 0.04f + Random.Shared.NextSingle() * 0.06f;

                ref var p = ref _particles[_count++];
                p.Position = pos;
                p.Velocity = dir * s;
                p.Life = 0f;
                p.MaxLife = lt;
                p.Size = sz;
                p.Rotation = Random.Shared.NextSingle() * MathF.Tau;
                p.AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * 6f;
                p.R = color.R;
                p.G = color.G;
                p.B = color.B;
                p.A = color.A;
            }
        }

        public void BurstSphere(Vector3 pos, int count, float speed, float gravity, float lifetime,
            Color color)
        {
            for (int i = 0; i < count; i++)
            {
                if (_count >= _particles.Length) RecycleOldest();

                float u = Random.Shared.NextSingle() * 2f - 1f;
                float theta = Random.Shared.NextSingle() * MathF.Tau;
                float r = MathF.Sqrt(1f - u * u);

                var dir = new Vector3(r * MathF.Cos(theta), u, r * MathF.Sin(theta));

                float s = speed * (0.5f + Random.Shared.NextSingle());
                float lt = lifetime * (0.5f + Random.Shared.NextSingle());
                float sz = 0.03f + Random.Shared.NextSingle() * 0.07f;

                ref var p = ref _particles[_count++];
                p.Position = pos;
                p.Velocity = dir * s;
                p.Life = 0f;
                p.MaxLife = lt;
                p.Size = sz;
                p.Rotation = Random.Shared.NextSingle() * MathF.Tau;
                p.AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * 6f;
                p.R = color.R;
                p.G = color.G;
                p.B = color.B;
                p.A = color.A;
            }
        }

        public void Update(float dt)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                ref var p = ref _particles[i];
                p.Life += dt;

                if (p.Life >= p.MaxLife)
                {
                    RemoveAt(i);
                    continue;
                }

                p.Velocity.Y -= 9.81f * dt;
                p.Position += p.Velocity * dt;
                p.Rotation += p.AngularVelocity * dt;
            }
        }

        public void Render()
        {
            Raylib.BeginBlendMode(BlendMode.Additive);

            for (int i = 0; i < _count; i++)
            {
                ref var p = ref _particles[i];
                float t = p.Life / p.MaxLife;
                float fade = 1f - t * t;
                float shrink = 1f - t * 0.7f;
                float size = p.Size * shrink;

                if (size < 0.001f) continue;

                float whiteBlend = MathF.Max(0f, 1f - t * 4f);
                byte cr = (byte)(p.R + (255 - p.R) * whiteBlend);
                byte cg = (byte)(p.G + (255 - p.G) * whiteBlend);
                byte cb = (byte)(p.B + (255 - p.B) * whiteBlend);
                byte a = (byte)Math.Clamp((int)(p.A * fade), 0, 255);

                var drawPos = new System.Numerics.Vector3(p.Position.X, p.Position.Y, p.Position.Z);

                Raylib.DrawCube(drawPos, size, size, size,
                    new Color(cr, cg, cb, a));

                if (fade > 0.3f)
                {
                    byte glowA = (byte)Math.Clamp((int)(a * 0.3f), 0, 255);
                    Raylib.DrawCube(drawPos, size * 1.8f, size * 1.8f, size * 1.8f,
                        new Color(cr, cg, cb, glowA));
                }
            }

            Raylib.EndBlendMode();
        }

        public void Clear()
        {
            _count = 0;
        }

        private void RecycleOldest()
        {
            float oldest = 0f;
            int idx = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_particles[i].Life > oldest)
                {
                    oldest = _particles[i].Life;
                    idx = i;
                }
            }
            RemoveAt(idx);
        }

        private void RemoveAt(int idx)
        {
            int last = _count - 1;
            if (idx < last)
                _particles[idx] = _particles[last];
            _count--;
        }
    }
}
