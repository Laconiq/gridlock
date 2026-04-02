using System;
using System.Numerics;
using Gridlock.Core;
using Raylib_cs;

namespace Gridlock.UI
{
    public sealed class HUD
    {
        private float _statePulse;
        private float _pulseDir = 1f;

        public bool WaveStartRequested { get; private set; }

        public void Render(GameState state, int wave, int totalWaves, int kills,
            float hp, float maxHP, int enemiesAlive, int towerCount, int maxTowers)
        {
            WaveStartRequested = false;
            float dt = Raylib.GetFrameTime();

            _statePulse += dt * _pulseDir * 2f;
            if (_statePulse > 1f) { _statePulse = 1f; _pulseDir = -1f; }
            if (_statePulse < 0.4f) { _statePulse = 0.4f; _pulseDir = 1f; }

            DrawTopBar(state, wave, totalWaves, kills, hp, maxHP, enemiesAlive);

            if (state == GameState.Preparing)
                DrawBottomBar(towerCount, maxTowers);
        }

        private void DrawTopBar(GameState state, int wave, int totalWaves, int kills,
            float hp, float maxHP, int enemiesAlive)
        {
            int screenW = Raylib.GetScreenWidth();
            int barH = 52;

            Raylib.DrawRectangle(0, 0, screenW, barH, new Color(10, 15, 20, 217));

            DrawStateBadge(state, 12, barH);

            int hpBarX = (int)(screenW * 0.3f);
            int hpBarW = (int)(screenW * 0.4f);
            int hpBarY = 14;
            int hpBarH = 24;
            DrawHPBar(hp, maxHP, hpBarX, hpBarY, hpBarW, hpBarH);

            int rightX = screenW - 12;
            int textY = 18;
            int fontSize = 20;

            if (state == GameState.Wave && enemiesAlive > 0)
            {
                var enemyText = $"ENEMIES: {enemiesAlive}";
                int tw = Raylib.MeasureText(enemyText, fontSize);
                rightX -= tw;
                Raylib.DrawText(enemyText, rightX, textY, fontSize, DesignTokens.Tertiary);
                rightX -= 20;
            }

            var killText = $"KILLS: {kills}";
            int killW = Raylib.MeasureText(killText, fontSize);
            rightX -= killW;
            Raylib.DrawText(killText, rightX, textY, fontSize, DesignTokens.OnSurfaceVariant);
            rightX -= 20;

            var waveText = $"WAVE {wave + 1}/{totalWaves}";
            int waveW = Raylib.MeasureText(waveText, fontSize);
            rightX -= waveW;
            Raylib.DrawText(waveText, rightX, textY, fontSize, DesignTokens.Primary);

            Raylib.DrawLine(0, barH - 1, screenW, barH - 1, DesignTokens.GlassBorderAccent);
        }

        private void DrawStateBadge(GameState state, int x, int barH)
        {
            string label = state switch
            {
                GameState.Preparing => "PREPARING",
                GameState.Wave => "WAVE",
                GameState.GameOver => "GAME OVER",
                _ => state.ToString().ToUpperInvariant()
            };

            var badgeColor = state switch
            {
                GameState.Preparing => DesignTokens.Primary,
                GameState.Wave => DesignTokens.Secondary,
                GameState.GameOver => DesignTokens.Error,
                _ => DesignTokens.OnSurface
            };

            int fontSize = 20;
            int textW = Raylib.MeasureText(label, fontSize);
            int padX = 10;
            int padY = 4;
            int badgeY = (barH - fontSize - padY * 2) / 2;
            int badgeW = textW + padX * 2;
            int badgeH = fontSize + padY * 2;

            float pulse = _statePulse;
            byte glowA = (byte)(38 * pulse);
            Raylib.DrawRectangle(x, badgeY, badgeW, badgeH,
                new Color(badgeColor.R, badgeColor.G, badgeColor.B, glowA));

            byte borderA = (byte)(153 * pulse);
            Raylib.DrawRectangleLines(x, badgeY, badgeW, badgeH,
                new Color(badgeColor.R, badgeColor.G, badgeColor.B, borderA));

            Raylib.DrawText(label, x + padX, badgeY + padY, fontSize, badgeColor);
        }

        private void DrawHPBar(float hp, float maxHP, int x, int y, int w, int h)
        {
            float frac = maxHP > 0f ? Math.Clamp(hp / maxHP, 0f, 1f) : 0f;

            var fillColor = frac > 0.5f ? DesignTokens.HudHpColor
                : frac > 0.25f ? DesignTokens.HudHpLow
                : DesignTokens.HudHpCritical;

            Raylib.DrawRectangle(x, y, w, h, DesignTokens.HudHpTrack);

            if (frac > 0f)
            {
                int fillW = (int)(w * frac);
                Raylib.DrawRectangle(x, y, fillW, h, fillColor);

                int highlightH = (int)(h * 0.35f);
                byte highlightA = (byte)(fillColor.A * 0.3f);
                Raylib.DrawRectangle(x, y, fillW, highlightH,
                    new Color(fillColor.R, fillColor.G, fillColor.B, highlightA));
            }

            byte borderA = (byte)(fillColor.A / 2);
            Raylib.DrawRectangleLines(x, y, w, h,
                new Color(fillColor.R, fillColor.G, fillColor.B, borderA));

            var hpText = $"HP {hp:F0} / {maxHP:F0}";
            int fontSize = 20;
            int textW = Raylib.MeasureText(hpText, fontSize);
            Raylib.DrawText(hpText, x + (w - textW) / 2, y + (h - fontSize) / 2,
                fontSize, DesignTokens.OnSurface);
        }

        private void DrawBottomBar(int towerCount, int maxTowers)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            int barH = 80;
            int barY = screenH - barH;

            int btnW = 220;
            int btnH = 50;
            int centerX = (screenW - btnW) / 2;
            int btnY = barY + (barH - btnH) / 2 - 8;

            var mousePos = Raylib.GetMousePosition();
            bool hover = mousePos.X >= centerX && mousePos.X <= centerX + btnW
                      && mousePos.Y >= btnY && mousePos.Y <= btnY + btnH;
            bool clicked = hover && Raylib.IsMouseButtonPressed(MouseButton.Left);

            byte bgA = hover ? (byte)77 : (byte)38;
            Raylib.DrawRectangle(centerX, btnY, btnW, btnH,
                new Color(DesignTokens.PrimaryContainer.R, DesignTokens.PrimaryContainer.G,
                    DesignTokens.PrimaryContainer.B, bgA));

            byte borderA = (byte)(204 * _statePulse);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(centerX, btnY, btnW, btnH),
                1.5f,
                new Color(DesignTokens.Primary.R, DesignTokens.Primary.G,
                    DesignTokens.Primary.B, borderA));

            int fontSize = 24;
            var btnText = "START WAVE";
            int textW = Raylib.MeasureText(btnText, fontSize);
            Raylib.DrawText(btnText, centerX + (btnW - textW) / 2, btnY + (btnH - fontSize) / 2,
                fontSize, DesignTokens.Primary);

            if (clicked)
                WaveStartRequested = true;

            var towerText = $"Towers: {towerCount}/{maxTowers}";
            int towerTextW = Raylib.MeasureText(towerText, 20);
            Raylib.DrawText(towerText, (screenW - towerTextW) / 2, btnY + btnH + 2,
                20, DesignTokens.OnSurfaceVariant);
        }
    }
}
