using System;
using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace Gridlock.UI
{
    public sealed class GameOverScreen
    {
        public bool RestartRequested { get; private set; }

        private float _fadeIn;
        private float _glitchTimer;

        public void Reset()
        {
            _fadeIn = 0f;
            _glitchTimer = 0f;
        }

        public void Render(int wave, int kills)
        {
            RestartRequested = false;
            float dt = Raylib.GetFrameTime();
            _fadeIn = MathF.Min(_fadeIn + dt * 2f, 1f);
            _glitchTimer += dt;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(screenW, screenH));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.02f, 0.03f, 0.05f, 0.88f * _fadeIn));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            ImGui.Begin("##GameOver", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            var drawList = ImGui.GetWindowDrawList();
            float centerX = screenW * 0.5f;
            float opacity = _fadeIn;

            DrawScanlines(drawList, screenW, screenH, opacity * 0.04f);

            DrawTitle(drawList, centerX, screenH * 0.3f, opacity);

            DrawStatsPanel(drawList, centerX, screenH * 0.44f, wave, kills, opacity);

            DrawRestartButton(centerX, screenH * 0.62f, opacity);

            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }

        private void DrawScanlines(ImDrawListPtr drawList, int screenW, int screenH, float alpha)
        {
            var lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha));
            for (int y = 0; y < screenH; y += 3)
            {
                drawList.AddLine(
                    new Vector2(0, y),
                    new Vector2(screenW, y),
                    lineColor);
            }
        }

        private void DrawTitle(ImDrawListPtr drawList, float cx, float y, float opacity)
        {
            var title = "GAME OVER";
            ImGui.SetWindowFontScale(3f);
            var titleSize = ImGui.CalcTextSize(title);
            ImGui.SetWindowFontScale(1f);

            float glitch = MathF.Sin(_glitchTimer * 12f) * 2f * MathF.Max(0f, 1f - _fadeIn * 2f);

            var titleColor = TV4A(DesignTokens.Primary, opacity);
            var shadowColor = TV4A(DesignTokens.Error, opacity * 0.4f);

            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * 3f,
                new Vector2(cx - titleSize.X * 0.5f + glitch + 2, y + 2),
                ImGui.ColorConvertFloat4ToU32(shadowColor), title);

            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * 3f,
                new Vector2(cx - titleSize.X * 0.5f + glitch, y),
                ImGui.ColorConvertFloat4ToU32(titleColor), title);

            float lineW = titleSize.X * 0.8f;
            float lineY = y + titleSize.Y + 8;
            drawList.AddLine(
                new Vector2(cx - lineW * 0.5f, lineY),
                new Vector2(cx + lineW * 0.5f, lineY),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.GlassBorderAccent, opacity)), 1.5f);
        }

        private void DrawStatsPanel(ImDrawListPtr drawList, float cx, float y,
            int wave, int kills, float opacity)
        {
            float panelW = 260;
            float panelH = 90;
            float px = cx - panelW * 0.5f;

            drawList.AddRectFilled(
                new Vector2(px, y),
                new Vector2(px + panelW, y + panelH),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.SurfaceContainerHigh, 0.6f * opacity)), 6f);

            drawList.AddRect(
                new Vector2(px, y),
                new Vector2(px + panelW, y + panelH),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.GlassBorder, opacity)), 6f, ImDrawFlags.None, 1f);

            float textY = y + 16;

            var waveLabel = "WAVES SURVIVED";
            var waveLabelSize = ImGui.CalcTextSize(waveLabel);
            drawList.AddText(
                new Vector2(cx - waveLabelSize.X * 0.5f, textY),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OnSurfaceVariant, opacity)), waveLabel);

            var waveValue = $"{wave + 1}";
            ImGui.SetWindowFontScale(1.5f);
            var waveValueSize = ImGui.CalcTextSize(waveValue);
            ImGui.SetWindowFontScale(1f);
            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * 1.5f,
                new Vector2(cx - waveValueSize.X * 0.75f, textY + 16),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.Primary, opacity)), waveValue);

            float killY = textY + 44;
            var killLabel = "TOTAL KILLS";
            var killLabelSize = ImGui.CalcTextSize(killLabel);
            drawList.AddText(
                new Vector2(cx - killLabelSize.X * 0.5f, killY),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OnSurfaceVariant, opacity)), killLabel);

            var killValue = $"{kills}";
            var killValueSize = ImGui.CalcTextSize(killValue);
            drawList.AddText(
                new Vector2(cx + killLabelSize.X * 0.5f + 8, killY),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OnSurface, opacity)), killValue);
        }

        private void DrawRestartButton(float cx, float y, float opacity)
        {
            if (opacity < 0.5f) return;

            float btnW = 180;
            float btnH = 48;

            ImGui.SetCursorPos(new Vector2(cx - btnW * 0.5f, y));

            ImGui.PushStyleColor(ImGuiCol.Button, TV4A(DesignTokens.PrimaryContainer, 0.15f * opacity));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.Primary, 0.3f * opacity));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.PrimaryDim, 0.5f * opacity));
            ImGui.PushStyleColor(ImGuiCol.Text, TV4A(DesignTokens.Primary, opacity));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.5f);
            ImGui.PushStyleColor(ImGuiCol.Border, TV4A(DesignTokens.Primary, 0.5f * opacity));

            ImGui.SetWindowFontScale(1.2f);
            if (ImGui.Button("RESTART", new Vector2(btnW, btnH)))
                RestartRequested = true;
            ImGui.SetWindowFontScale(1f);

            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(2);
        }

        private static Vector4 TV4A(Color c, float alpha) =>
            new(c.R / 255f, c.G / 255f, c.B / 255f, alpha);
    }
}
