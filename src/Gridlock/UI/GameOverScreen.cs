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
        private float _sessionTime;
        private string _logId = "";

        private static float Scale => Raylib.GetScreenHeight() / 1080f;

        public void Reset()
        {
            _fadeIn = 0f;
            _glitchTimer = 0f;
            _sessionTime = (float)Raylib_cs.Raylib.GetTime();
            _logId = $"VP_FAIL_{Random.Shared.Next(0, 999):D3}_{Random.Shared.Next(0, 99):D2}";
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

            DrawScanlines(drawList, screenW, screenH, opacity * 0.03f);

            DrawDecorativeText(drawList, screenW, screenH, opacity);

            DrawBadge(drawList, centerX, screenH * 0.24f, opacity);
            DrawTitle(drawList, centerX, screenH * 0.28f, opacity);

            DrawBentoStats(drawList, centerX, screenH * 0.42f, wave, kills, opacity);

            DrawButtons(centerX, screenH * 0.60f, opacity);

            DrawFooter(drawList, centerX, screenH * 0.70f, opacity);

            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }

        private void DrawScanlines(ImDrawListPtr drawList, int screenW, int screenH, float alpha)
        {
            var lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha));
            for (int y = 0; y < screenH; y += 3)
                drawList.AddLine(new Vector2(0, y), new Vector2(screenW, y), lineColor);
        }

        private void DrawDecorativeText(ImDrawListPtr drawList, int screenW, int screenH, float opacity)
        {
            float s = Scale;
            float dimAlpha = opacity * 0.15f;
            var dimColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, dimAlpha));

            float rx = screenW - 220 * s;
            float lx = 20 * s;
            float y0 = 20 * s;
            float dy = 14 * s;

            drawList.AddText(new Vector2(rx, y0), dimColor, "TRACE_ROUTE::127.0.0.1");
            drawList.AddText(new Vector2(rx, y0 + dy), dimColor, "PACKET_LOSS::100%");
            drawList.AddText(new Vector2(rx, y0 + dy * 2), dimColor, "SIGNAL_STRENGTH::0%");

            drawList.AddText(new Vector2(lx, y0), dimColor, "0000 0000 0000 0001");
            drawList.AddText(new Vector2(lx, y0 + dy), dimColor, "0010 1101 0000 1111");
            drawList.AddText(new Vector2(lx, y0 + dy * 2), dimColor, "1111 0000 1111 0000");
        }

        private void DrawBadge(ImDrawListPtr drawList, float cx, float y, float opacity)
        {
            var label = "HARDWARE_TIMEOUT";
            var labelSize = ImGui.CalcTextSize(label);
            float padX = 10, padH = 6;
            float bx = cx - (labelSize.X + padX * 2) * 0.5f;
            float bw = labelSize.X + padX * 2;
            float bh = labelSize.Y + padH * 2;

            drawList.AddRectFilled(
                new Vector2(bx, y), new Vector2(bx + bw, y + bh),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.ErrorContainer, 0.8f * opacity)), 3f);
            drawList.AddText(
                new Vector2(bx + padX, y + padH),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.Error, opacity)), label);
        }

        private void DrawTitle(ImDrawListPtr drawList, float cx, float y, float opacity)
        {
            float s = Scale;
            var title = "SYSTEM_FAILURE";
            float titleScale = 3f * s;
            ImGui.SetWindowFontScale(titleScale);
            var titleSize = ImGui.CalcTextSize(title);
            ImGui.SetWindowFontScale(1f);

            float glitch = MathF.Sin(_glitchTimer * 12f) * 2f * MathF.Max(0f, 1f - _fadeIn * 2f);

            var shadowColor = TV4A(DesignTokens.Error, opacity * 0.3f);
            var titleColor = TV4A(DesignTokens.OnSurface, opacity);

            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * titleScale,
                new Vector2(cx - titleSize.X * 0.5f + glitch + 2, y + 2),
                ImGui.ColorConvertFloat4ToU32(shadowColor), title);

            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * titleScale,
                new Vector2(cx - titleSize.X * 0.5f + glitch, y),
                ImGui.ColorConvertFloat4ToU32(titleColor), title);

            var subtitle = "VOID_PROTOCOL_CONNECTION_DROPPED";
            var subSize = ImGui.CalcTextSize(subtitle);
            float subY = y + titleSize.Y + 6;
            drawList.AddText(
                new Vector2(cx - subSize.X * 0.5f, subY),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OutlineVariant, opacity)), subtitle);

            float lineY = subY + subSize.Y + 8;
            float lineW = titleSize.X * 0.6f;
            drawList.AddLine(
                new Vector2(cx - lineW * 0.5f, lineY),
                new Vector2(cx + lineW * 0.5f, lineY),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.GlassBorderAccent, opacity)), 1f);
        }

        private void DrawBentoStats(ImDrawListPtr drawList, float cx, float y,
            int wave, int kills, float opacity)
        {
            float s = Scale;
            float boxW = 160 * s;
            float boxH = 72 * s;
            float gap = 4 * s;
            float totalW = boxW * 3 + gap * 2;
            float startX = cx - totalW * 0.5f;

            float elapsed = (float)Raylib.GetTime() - _sessionTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);

            DrawStatBox(drawList, startX, y, boxW, boxH,
                DesignTokens.Primary, "TOTAL_UPTIME", $"{minutes:D2}:{seconds:D2}", "SESSION_ELAPSED", opacity);

            DrawStatBox(drawList, startX + boxW + gap, y, boxW, boxH,
                DesignTokens.Secondary, "WAVE_REACHED", $"{wave + 1:D2}", "MAX_DEPTH", opacity);

            DrawStatBox(drawList, startX + (boxW + gap) * 2, y, boxW, boxH,
                DesignTokens.Tertiary, "HOSTILES_NEUTRALIZED", $"{kills}", "TOTAL_KILLS", opacity);
        }

        private void DrawStatBox(ImDrawListPtr drawList, float x, float y, float w, float h,
            Color accent, string label, string value, string sub, float opacity)
        {
            drawList.AddRectFilled(
                new Vector2(x, y), new Vector2(x + w, y + h),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.SurfaceContainerHigh, 0.6f * opacity)), 4f);

            float s = Scale;
            drawList.AddRectFilled(
                new Vector2(x, y + 4 * s), new Vector2(x + 3 * s, y + h - 4 * s),
                ImGui.ColorConvertFloat4ToU32(TV4A(accent, 0.8f * opacity)));

            float pad = 12 * s;
            drawList.AddText(new Vector2(x + pad, y + 8 * s),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OnSurfaceVariant, opacity * 0.7f)), label);

            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize() * 1.8f * s,
                new Vector2(x + pad, y + 24 * s),
                ImGui.ColorConvertFloat4ToU32(TV4A(accent, opacity)), value);

            drawList.AddText(new Vector2(x + pad, y + h - 18 * s),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OutlineVariant, opacity * 0.5f)), sub);
        }

        private void DrawButtons(float cx, float y, float opacity)
        {
            if (opacity < 0.5f) return;

            float s = Scale;
            float btnW = 200 * s;
            float btnH = 40 * s;

            ImGui.SetCursorPos(new Vector2(cx - btnW * 0.5f, y));

            ImGui.PushStyleColor(ImGuiCol.Button, TV4A(DesignTokens.Primary, 0.12f * opacity));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.Primary, 0.25f * opacity));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.PrimaryDim, 0.4f * opacity));
            ImGui.PushStyleColor(ImGuiCol.Text, TV4A(DesignTokens.OnSurface, opacity));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.PushStyleColor(ImGuiCol.Border, TV4A(DesignTokens.Primary, 0.4f * opacity));

            if (ImGui.Button("REBOOT_SYSTEM", new Vector2(btnW, btnH)))
                RestartRequested = true;

            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(2);
        }

        private void DrawFooter(ImDrawListPtr drawList, float cx, float y, float opacity)
        {
            if (opacity < 0.3f) return;

            float dimAlpha = opacity * 0.3f;
            var dimColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, dimAlpha));

            var logText = $"LOG_ID::{_logId}";
            var logSize = ImGui.CalcTextSize(logText);
            drawList.AddText(new Vector2(cx - logSize.X * 0.5f, y), dimColor, logText);

            float s = Scale;
            float dotY = y + 18 * s;
            float dotSpacing = 10 * s;
            float dotStartX = cx - dotSpacing;

            float dotR = 3f * s;
            drawList.AddCircleFilled(new Vector2(dotStartX, dotY),
                dotR, ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.Error, opacity * 0.6f)));
            drawList.AddCircleFilled(new Vector2(dotStartX + dotSpacing, dotY),
                dotR, ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, opacity * 0.4f)));
            drawList.AddCircleFilled(new Vector2(dotStartX + dotSpacing * 2, dotY),
                dotR, ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, opacity * 0.4f)));
        }

        private static Vector4 TV4A(Color c, float alpha) =>
            new(c.R / 255f, c.G / 255f, c.B / 255f, alpha);
    }
}
