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

        private static float Scale => Raylib.GetScreenHeight() / 1080f;

        private static int S(int px) => (int)(px * Scale);

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
            int barH = S(52);

            Raylib.DrawRectangle(0, 0, screenW, barH, new Color(4, 8, 12, 230));
            Raylib.DrawLine(0, barH - 1, screenW, barH - 1, DesignTokens.GlassBorderAccent);

            int lx = S(14);
            int centerY = barH / 2;

            // Status dot
            byte dotAlpha = (byte)(180 + 75 * _statePulse);
            var dotColor = state switch
            {
                GameState.Wave => DesignTokens.Secondary,
                GameState.GameOver => DesignTokens.Error,
                _ => DesignTokens.Primary
            };
            Raylib.DrawCircle(lx + S(5), centerY, S(5), new Color(dotColor.R, dotColor.G, dotColor.B, dotAlpha));
            lx += S(18);

            // Branding
            int brandFont = S(18);
            Raylib.DrawText("GRIDLOCK", lx, centerY - brandFont / 2, brandFont, DesignTokens.Primary);
            lx += Raylib.MeasureText("GRIDLOCK", brandFont) + S(8);
            int smallFont = S(12);
            Raylib.DrawText("v3.0", lx, centerY - smallFont / 2 + S(1), smallFont, DesignTokens.OutlineVariant);

            // Center stats
            DrawCenterStats(state, wave, totalWaves, kills, hp, maxHP, enemiesAlive, screenW, barH);

            // Right: operator + signal
            DrawOperatorStatus(state, screenW, barH);
        }

        private void DrawCenterStats(GameState state, int wave, int totalWaves, int kills,
            float hp, float maxHP, int enemiesAlive, int screenW, int barH)
        {
            int cx = screenW / 2;
            int y = S(6);
            int statH = barH - S(12);
            float hpFrac = maxHP > 0 ? Math.Clamp(hp / maxHP, 0f, 1f) : 0f;

            int hpBlockW = S(200);
            int waveBlockW = S(130);
            int killBlockW = S(100);
            int hostilesBlockW = state == GameState.Wave ? S(130) : 0;
            int gap = S(16);

            int totalW = hpBlockW + gap + waveBlockW + gap + killBlockW;
            if (hostilesBlockW > 0)
                totalW += gap + hostilesBlockW;

            int startX = cx - totalW / 2;
            int labelFont = S(11);
            int valueFont = S(20);

            // HP block
            DrawStatBlock_HP(startX, y, hpBlockW, statH, hp, maxHP, hpFrac);
            startX += hpBlockW + gap;

            // Divider
            Raylib.DrawLine(startX - gap / 2, y + S(4), startX - gap / 2, y + statH - S(4), DesignTokens.OutlineVariant);

            // Wave block
            Raylib.DrawText("WAVE", startX, y + S(2), labelFont, DesignTokens.OnSurfaceVariant);
            var waveVal = $"{wave + 1:D2} / {totalWaves:D2}";
            Raylib.DrawText(waveVal, startX, y + S(16), valueFont, DesignTokens.Primary);
            startX += waveBlockW;

            // Divider
            Raylib.DrawLine(startX - gap / 2, y + S(4), startX - gap / 2, y + statH - S(4), DesignTokens.OutlineVariant);

            // Kills block
            Raylib.DrawText("KILLS", startX, y + S(2), labelFont, DesignTokens.OnSurfaceVariant);
            Raylib.DrawText($"{kills}", startX, y + S(16), valueFont, DesignTokens.OnSurface);
            startX += killBlockW;

            // Hostiles block (wave only)
            if (state == GameState.Wave)
            {
                Raylib.DrawLine(startX + gap / 2, y + S(4), startX + gap / 2, y + statH - S(4), DesignTokens.OutlineVariant);
                startX += gap;

                Raylib.DrawText("HOSTILES", startX, y + S(2), labelFont, DesignTokens.OnSurfaceVariant);
                var hostileColor = enemiesAlive > 0 ? DesignTokens.Secondary : DesignTokens.OutlineVariant;
                Raylib.DrawText($"{enemiesAlive}", startX, y + S(16), valueFont, hostileColor);
            }
        }

        private void DrawStatBlock_HP(int x, int y, int w, int h, float hp, float maxHP, float frac)
        {
            int labelFont = S(11);
            int smallFont = S(12);

            Raylib.DrawText("LIVES", x, y + S(2), labelFont, DesignTokens.OnSurfaceVariant);

            int barY = y + S(16);
            int barH = S(8);
            int barW = w - S(55);

            var fillColor = frac > 0.5f ? DesignTokens.HudHpColor
                : frac > 0.25f ? DesignTokens.HudHpLow
                : DesignTokens.HudHpCritical;

            Raylib.DrawRectangle(x, barY, barW, barH, DesignTokens.HudHpTrack);
            if (frac > 0f)
            {
                int fillW = (int)(barW * frac);
                Raylib.DrawRectangle(x, barY, fillW, barH, fillColor);
                int highlightH = (int)(barH * 0.35f);
                Raylib.DrawRectangle(x, barY, fillW, highlightH,
                    new Color((byte)255, (byte)255, (byte)255, (byte)40));
            }

            var pctText = $"{(int)(frac * 100)}%";
            Raylib.DrawText(pctText, x + barW + S(6), barY - S(1), smallFont, fillColor);

            var hpText = $"{hp:F0}/{maxHP:F0}";
            Raylib.DrawText(hpText, x, barY + barH + S(3), S(10), DesignTokens.OnSurfaceVariant);
        }

        private void DrawOperatorStatus(GameState state, int screenW, int barH)
        {
            int rx = screenW - S(14);
            int cy = barH / 2;
            int smallFont = S(12);

            bool online = state != GameState.GameOver;
            var sigColor = online ? DesignTokens.Secondary : DesignTokens.Error;
            var sigText = online ? "NOMINAL" : "OFFLINE";

            int sigW = Raylib.MeasureText(sigText, smallFont);
            rx -= sigW;
            Raylib.DrawText(sigText, rx, cy + S(2), smallFont, sigColor);

            rx -= S(12);
            Raylib.DrawCircle(rx, cy + S(6), S(3), sigColor);

            rx -= S(10);
            var opText = "OPERATOR_01";
            int opW = Raylib.MeasureText(opText, smallFont);
            rx -= opW;
            Raylib.DrawText(opText, rx, cy - S(6), smallFont, DesignTokens.OnSurfaceVariant);
        }

        private void DrawBottomBar(int towerCount, int maxTowers)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            int btnW = S(240);
            int btnH = S(48);
            int centerX = (screenW - btnW) / 2;
            int btnY = screenH - S(100);

            // Glass panel
            int pad = S(16);
            Raylib.DrawRectangle(centerX - pad, btnY - pad,
                btnW + pad * 2, btnH + pad * 2 + S(24),
                new Color(4, 8, 12, 180));
            Raylib.DrawRectangleLinesEx(
                new Rectangle(centerX - pad, btnY - pad,
                    btnW + pad * 2, btnH + pad * 2 + S(24)),
                1f, DesignTokens.GlassBorder);

            var mousePos = Raylib.GetMousePosition();
            bool hover = mousePos.X >= centerX && mousePos.X <= centerX + btnW
                      && mousePos.Y >= btnY && mousePos.Y <= btnY + btnH;
            bool clicked = hover && Raylib.IsMouseButtonPressed(MouseButton.Left);

            byte bgA = hover ? (byte)50 : (byte)20;
            Raylib.DrawRectangle(centerX, btnY, btnW, btnH,
                new Color(DesignTokens.PrimaryContainer.R, DesignTokens.PrimaryContainer.G,
                    DesignTokens.PrimaryContainer.B, bgA));

            byte borderA = (byte)(180 * _statePulse);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(centerX, btnY, btnW, btnH),
                1.5f,
                new Color(DesignTokens.Primary.R, DesignTokens.Primary.G,
                    DesignTokens.Primary.B, borderA));

            int fontSize = S(18);
            var btnText = "[ START WAVE ]";
            int textW = Raylib.MeasureText(btnText, fontSize);
            Raylib.DrawText(btnText, centerX + (btnW - textW) / 2, btnY + (btnH - fontSize) / 2,
                fontSize, DesignTokens.Primary);

            if (clicked)
                WaveStartRequested = true;

            int smallFont = S(12);
            var towerText = $"TOWERS {towerCount}/{maxTowers}";
            int towerTextW = Raylib.MeasureText(towerText, smallFont);
            Raylib.DrawText(towerText, (screenW - towerTextW) / 2, btnY + btnH + S(8),
                smallFont, DesignTokens.OnSurfaceVariant);
        }
    }
}
