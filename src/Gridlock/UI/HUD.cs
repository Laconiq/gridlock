using System;
using System.Numerics;
using Gridlock.Core;
using ImGuiNET;
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
            float barH = 52;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(screenW, barH));
            PushBarStyle();

            ImGui.Begin("##HUD_Top", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            var drawList = ImGui.GetWindowDrawList();

            DrawStateBadge(drawList, state, 12, barH);

            float hpBarX = screenW * 0.3f;
            float hpBarW = screenW * 0.4f;
            float hpBarY = 14;
            float hpBarH = 24;
            DrawHPBar(drawList, hp, maxHP, hpBarX, hpBarY, hpBarW, hpBarH);

            float rightX = screenW - 12f;

            if (state == GameState.Wave && enemiesAlive > 0)
            {
                var enemyText = $"ENEMIES: {enemiesAlive}";
                var enemySize = ImGui.CalcTextSize(enemyText);
                rightX -= enemySize.X;
                drawList.AddText(new Vector2(rightX, 18),
                    ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.Tertiary)), enemyText);
                rightX -= 20;
            }

            var killText = $"KILLS: {kills}";
            var killSize = ImGui.CalcTextSize(killText);
            rightX -= killSize.X;
            drawList.AddText(new Vector2(rightX, 18),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OnSurfaceVariant)), killText);
            rightX -= 20;

            var waveText = $"WAVE {wave + 1}/{totalWaves}";
            var waveSize = ImGui.CalcTextSize(waveText);
            rightX -= waveSize.X;
            drawList.AddText(new Vector2(rightX, 18),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.Primary)), waveText);

            drawList.AddLine(
                new Vector2(0, barH - 1),
                new Vector2(screenW, barH - 1),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.GlassBorderAccent)));

            ImGui.End();
            PopBarStyle();
        }

        private void DrawStateBadge(ImDrawListPtr drawList, GameState state, float x, float barH)
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

            float pulse = _statePulse;
            var glowColor = TV4A(badgeColor, 0.15f * pulse);

            var textSize = ImGui.CalcTextSize(label);
            float badgePadX = 10;
            float badgePadY = 4;
            float badgeX = x;
            float badgeY = (barH - textSize.Y - badgePadY * 2) * 0.5f;
            float badgeW = textSize.X + badgePadX * 2;
            float badgeH = textSize.Y + badgePadY * 2;

            drawList.AddRectFilled(
                new Vector2(badgeX, badgeY),
                new Vector2(badgeX + badgeW, badgeY + badgeH),
                ImGui.ColorConvertFloat4ToU32(glowColor), 4f);

            drawList.AddRect(
                new Vector2(badgeX, badgeY),
                new Vector2(badgeX + badgeW, badgeY + badgeH),
                ImGui.ColorConvertFloat4ToU32(TV4A(badgeColor, 0.6f * pulse)), 4f, ImDrawFlags.None, 1.5f);

            drawList.AddText(
                new Vector2(badgeX + badgePadX, badgeY + badgePadY),
                ImGui.ColorConvertFloat4ToU32(TV4(badgeColor)), label);
        }

        private void DrawHPBar(ImDrawListPtr drawList, float hp, float maxHP,
            float x, float y, float w, float h)
        {
            float frac = maxHP > 0f ? Math.Clamp(hp / maxHP, 0f, 1f) : 0f;

            var fillColor = frac > 0.5f ? DesignTokens.HudHpColor
                : frac > 0.25f ? DesignTokens.HudHpLow
                : DesignTokens.HudHpCritical;

            drawList.AddRectFilled(
                new Vector2(x, y), new Vector2(x + w, y + h),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.HudHpTrack)), 3f);

            if (frac > 0f)
            {
                drawList.AddRectFilled(
                    new Vector2(x, y), new Vector2(x + w * frac, y + h),
                    ImGui.ColorConvertFloat4ToU32(TV4(fillColor)), 3f);

                drawList.AddRectFilled(
                    new Vector2(x, y), new Vector2(x + w * frac, y + h * 0.35f),
                    ImGui.ColorConvertFloat4ToU32(TV4A(fillColor, 0.3f)), 3f);
            }

            drawList.AddRect(
                new Vector2(x, y), new Vector2(x + w, y + h),
                ImGui.ColorConvertFloat4ToU32(TV4A(fillColor, 0.5f)), 3f, ImDrawFlags.None, 1f);

            var hpText = $"HP {hp:F0} / {maxHP:F0}";
            var textSize = ImGui.CalcTextSize(hpText);
            drawList.AddText(
                new Vector2(x + (w - textSize.X) * 0.5f, y + (h - textSize.Y) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OnSurface)), hpText);
        }

        private void DrawBottomBar(int towerCount, int maxTowers)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            float barH = 80;

            ImGui.SetNextWindowPos(new Vector2(0, screenH - barH));
            ImGui.SetNextWindowSize(new Vector2(screenW, barH));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            ImGui.Begin("##HUD_Bottom", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            float btnW = 220;
            float btnH = 50;
            float centerX = (screenW - btnW) * 0.5f;

            ImGui.SetCursorPos(new Vector2(centerX, (barH - btnH) * 0.5f - 8));

            ImGui.PushStyleColor(ImGuiCol.Button, TV4A(DesignTokens.PrimaryContainer, 0.15f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.Primary, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.PrimaryDim, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.Text, TV4(DesignTokens.Primary));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.5f);
            ImGui.PushStyleColor(ImGuiCol.Border, TV4A(DesignTokens.Primary, _statePulse * 0.8f));

            ImGui.SetWindowFontScale(1.2f);
            if (ImGui.Button("START WAVE", new Vector2(btnW, btnH)))
                WaveStartRequested = true;
            ImGui.SetWindowFontScale(1f);

            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(2);

            var towerText = $"Towers: {towerCount}/{maxTowers}";
            var towerSize = ImGui.CalcTextSize(towerText);
            ImGui.SetCursorPos(new Vector2((screenW - towerSize.X) * 0.5f, (barH - btnH) * 0.5f + btnH - 2));
            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), towerText);

            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }

        private static void PushBarStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.04f, 0.06f, 0.08f, 0.85f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0f, 0.8f, 1f, 0.3f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        }

        private static void PopBarStyle()
        {
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(2);
        }

        private static Vector4 TV4(Color c) =>
            new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

        private static Vector4 TV4A(Color c, float alpha) =>
            new(c.R / 255f, c.G / 255f, c.B / 255f, alpha);
    }
}
