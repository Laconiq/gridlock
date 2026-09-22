using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class DamageTextSystem
    {
        public static DamageTextSystem? Instance { get; private set; }

        private const int MaxTexts = 64;
        private const float Duration = 0.8f;
        private const float RiseSpeed = 80f;

        private struct Entry
        {
            public Vector3 WorldPos;
            public string Text;
            public float Age;
            public Color Color;
        }

        private readonly Entry[] _entries = new Entry[MaxTexts];
        private int _count;

        public void Init() { Instance = this; }
        public void Shutdown() { Instance = null; }

        public void Spawn(Vector3 pos, float damage, Color color)
        {
            pos.X += (Random.Shared.NextSingle() - 0.5f) * 0.3f;
            pos.Y += 0.5f + Random.Shared.NextSingle() * 0.2f;
            pos.Z += (Random.Shared.NextSingle() - 0.5f) * 0.3f;

            if (_count >= MaxTexts)
            {
                float oldest = 0f;
                int idx = 0;
                for (int i = 0; i < _count; i++)
                {
                    if (_entries[i].Age >= oldest) { oldest = _entries[i].Age; idx = i; }
                }
                _entries[idx] = _entries[--_count];
            }

            _entries[_count++] = new Entry
            {
                WorldPos = pos,
                Text = ((int)damage).ToString(),
                Age = 0f,
                Color = color
            };
        }

        public void Update(float dt)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                _entries[i].Age += dt;
                if (_entries[i].Age >= Duration)
                {
                    int last = _count - 1;
                    if (i < last) _entries[i] = _entries[last];
                    _count--;
                }
            }
        }

        public void Render(Camera3D camera)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var e = ref _entries[i];
                float t = e.Age / Duration;
                float alpha = 1f - t * t;

                var screenPos = Raylib.GetWorldToScreen(e.WorldPos, camera);
                float offsetY = -RiseSpeed * e.Age;

                float scale = HudScale();
                int fontSize = (int)(14 * scale);
                if (fontSize < 10) fontSize = 10;

                byte a = (byte)Math.Clamp((int)(255 * alpha), 0, 255);
                var color = new Color(e.Color.R, e.Color.G, e.Color.B, a);

                int textW = Raylib.MeasureText(e.Text, fontSize);
                int x = (int)(screenPos.X - textW / 2);
                int y = (int)(screenPos.Y + offsetY);

                if (x < -100 || x > Raylib.GetScreenWidth() + 100) continue;
                if (y < -100 || y > Raylib.GetScreenHeight() + 100) continue;

                byte shadowA = (byte)Math.Clamp((int)(120 * alpha), 0, 255);
                Raylib.DrawText(e.Text, x + 1, y + 1, fontSize, new Color((byte)0, (byte)0, (byte)0, shadowA));
                Raylib.DrawText(e.Text, x, y, fontSize, color);
            }
        }

        public void Clear() { _count = 0; }

        private static float HudScale() => Raylib.GetScreenHeight() / 1080f;
    }
}
