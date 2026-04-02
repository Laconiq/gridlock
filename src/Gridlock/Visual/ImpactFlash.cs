using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class ImpactFlash
    {
        public static ImpactFlash? Instance { get; private set; }

        private const int MaxFlashes = 32;

        private struct Flash
        {
            public Vector3 Pos;
            public float Life;
            public float MaxLife;
            public float Size;
            public Color Col;
        }

        private readonly Flash[] _flashes = new Flash[MaxFlashes];
        private int _count;

        public void Init() { Instance = this; }
        public void Shutdown() { Instance = null; }

        public void Spawn(Vector3 pos, Color color, float size = 0.4f, float duration = 0.15f)
        {
            if (_count >= MaxFlashes)
            {
                float oldest = 0f;
                int idx = 0;
                for (int i = 0; i < _count; i++)
                {
                    if (_flashes[i].Life > oldest)
                    {
                        oldest = _flashes[i].Life;
                        idx = i;
                    }
                }
                _flashes[idx] = _flashes[_count - 1];
                _count--;
            }

            _flashes[_count++] = new Flash
            {
                Pos = pos,
                Life = 0f,
                MaxLife = duration,
                Size = size,
                Col = color
            };
        }

        public void Update(float dt)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                _flashes[i].Life += dt;
                if (_flashes[i].Life >= _flashes[i].MaxLife)
                {
                    int last = _count - 1;
                    if (i < last)
                        _flashes[i] = _flashes[last];
                    _count--;
                }
            }
        }

        public void Render()
        {
            Raylib.BeginBlendMode(BlendMode.Additive);
            for (int i = 0; i < _count; i++)
            {
                ref var f = ref _flashes[i];
                float t = f.Life / f.MaxLife;
                float fade = 1f - t * t;
                float expand = 1f + t * 2f;

                float coreRadius = f.Size * expand * 0.4f;
                float midRadius = f.Size * expand * 0.7f;
                float outerRadius = f.Size * expand * 1.1f;

                var drawPos = new System.Numerics.Vector3(f.Pos.X, f.Pos.Y, f.Pos.Z);

                byte coreA = (byte)Math.Clamp((int)(200 * fade), 0, 255);
                Raylib.DrawSphere(drawPos, coreRadius,
                    new Color((byte)255, (byte)255, (byte)255, coreA));

                byte midA = (byte)Math.Clamp((int)(120 * fade), 0, 255);
                Raylib.DrawSphere(drawPos, midRadius,
                    new Color(f.Col.R, f.Col.G, f.Col.B, midA));

                byte outerA = (byte)Math.Clamp((int)(40 * fade), 0, 255);
                Raylib.DrawSphere(drawPos, outerRadius,
                    new Color(f.Col.R, f.Col.G, f.Col.B, outerA));
            }
            Raylib.EndBlendMode();
        }

        public void Clear() { _count = 0; }
    }
}
