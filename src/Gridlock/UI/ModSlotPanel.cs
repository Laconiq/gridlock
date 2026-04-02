using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Loot;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Gridlock.Towers;
using ImGuiNET;
using Raylib_cs;

namespace Gridlock.UI
{
    public sealed class ModSlotPanel
    {
        private static readonly TargetingMode[] CachedTargetingModes = Enum.GetValues<TargetingMode>();
        private static readonly string[] CachedTargetingNames = Enum.GetNames<TargetingMode>();
        private static readonly ModType[] CachedModTypes = Enum.GetValues<ModType>();
        private static readonly ModTags[] CachedModTags = Enum.GetValues<ModTags>();

        private Tower? _tower;
        private PlayerInventory? _inventory;
        private readonly List<ModSlotData> _workingSlots = new();
        private readonly List<SynergyEffect> _activeSynergies = new();
        private TargetingMode _workingTargetingMode;
        private ModType? _hoveredMod;
        private ModType _draggedMod;
        private bool _dragActive;
        private float _dragPulse;

        public bool IsOpen => _tower != null;

        public void Open(Tower tower, PlayerInventory inventory)
        {
            _tower = tower;
            _inventory = inventory;
            _workingSlots.Clear();
            foreach (var slot in tower.Executor.ModSlots)
                _workingSlots.Add(new ModSlotData { modType = slot.modType });
            _workingTargetingMode = tower.Executor.TargetingMode;
            _hoveredMod = null;
            RefreshSynergies();
        }

        public void Close()
        {
            if (_tower != null)
            {
                _tower.Executor.SetSlots(_workingSlots);
                _tower.Executor.TargetingMode = _workingTargetingMode;
            }
            _tower = null;
            _inventory = null;
            _workingSlots.Clear();
            _activeSynergies.Clear();
            _hoveredMod = null;
        }

        public void Render()
        {
            if (_tower == null || _inventory == null) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // --- Right panel: Player Inventory ---
            float invW = MathF.Min(220, screenW * 0.18f);
            float invH = screenH;

            ImGui.SetNextWindowPos(new Vector2(screenW - invW, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(invW, invH));
            PushPanelStyle();

            ImGui.Begin("##InventoryPanel", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);

            var dl = ImGui.GetWindowDrawList();
            var wPos = ImGui.GetWindowPos();
            dl.AddRectFilled(wPos, new Vector2(wPos.X + 2, wPos.Y + invH),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.GlassBorderAccent)));

            DrawInventoryPane();

            ImGui.End();
            PopPanelStyle();

            // --- Center panel: Tower Config ---
            float towerW = MathF.Min(560, screenW * 0.45f);
            float towerH = MathF.Min(screenH - 80, 380);
            float towerX = (screenW - invW - towerW) * 0.5f;
            float towerY = (screenH - towerH) * 0.5f;

            ImGui.SetNextWindowPos(new Vector2(towerX, towerY), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(towerW, towerH));
            PushPanelStyle();

            bool open = true;
            ImGui.Begin("TOWER CONFIG##TowerPanel", ref open,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove);

            if (!open)
            {
                Close();
                ImGui.End();
                PopPanelStyle();
                return;
            }

            DrawTowerHeader();
            ImGui.Spacing();
            DrawSlotChain();
            ImGui.Separator();
            ImGui.Spacing();
            DrawInfoArea();

            ImGui.End();
            PopPanelStyle();

            // --- Drag preview ---
            if (_dragActive)
            {
                _dragPulse += Raylib.GetFrameTime() * 6f;
                if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    _dragActive = false;
                    _dragPulse = 0f;
                }
                else
                {
                    DrawDragPreview();
                }
            }
        }

        private void DrawDragPreview()
        {
            var mousePos = ImGui.GetMousePos();
            var fg = ImGui.GetForegroundDrawList();
            var modColor = GetModColor(_draggedMod);
            float pulse = 0.8f + 0.2f * MathF.Sin(_dragPulse);

            float cardW = 70;
            float cardH = 32;
            float ox = mousePos.X + 12;
            float oy = mousePos.Y - cardH * 0.5f;

            fg.AddRectFilled(
                new Vector2(ox, oy), new Vector2(ox + cardW, oy + cardH),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.SurfaceContainerHigh, 0.9f * pulse)), 4f);

            fg.AddRectFilled(
                new Vector2(ox, oy), new Vector2(ox + 3, oy + cardH),
                ImGui.ColorConvertFloat4ToU32(TV4A(modColor, pulse)));

            fg.AddRect(
                new Vector2(ox, oy), new Vector2(ox + cardW, oy + cardH),
                ImGui.ColorConvertFloat4ToU32(TV4A(modColor, 0.6f * pulse)), 4f, ImDrawFlags.None, 1.5f);

            float iconSize = 10;
            fg.AddRectFilled(
                new Vector2(ox + 8, oy + (cardH - iconSize) * 0.5f),
                new Vector2(ox + 8 + iconSize, oy + (cardH + iconSize) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(TV4A(modColor, pulse)), 2f);

            fg.AddText(
                new Vector2(ox + 22, oy + (cardH - ImGui.GetFontSize()) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OnSurface, pulse)),
                _draggedMod.ToString().ToUpperInvariant());
        }

        private void DrawTowerHeader()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, TV4(DesignTokens.Primary));
            ImGui.SetWindowFontScale(1.3f);
            ImGui.Text("TOWER CONFIG");
            ImGui.SetWindowFontScale(1f);
            ImGui.PopStyleColor();

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 200);
            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), "Range:");
            ImGui.SameLine();
            ImGui.TextColored(TV4(DesignTokens.OnSurface), $"{_tower!.Data.BaseRange:F0}");

            ImGui.SameLine();
            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), "DMG:");
            ImGui.SameLine();

            float effectiveDmg = ComputeEffectiveDamage();
            ImGui.TextColored(TV4(DesignTokens.OnSurface), $"{effectiveDmg:F1}");

            ImGui.SameLine();
            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), "Rate:");
            ImGui.SameLine();

            float effectiveRate = ComputeEffectiveFireRate();
            ImGui.TextColored(TV4(DesignTokens.OnSurface), $"{effectiveRate:F1}/s");

            ImGui.Spacing();

            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), "TARGETING");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            int current = (int)_workingTargetingMode;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, TV4(DesignTokens.SurfaceContainerHigh));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, TV4(DesignTokens.GlassInnerBgHover));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, TV4(DesignTokens.SurfaceContainerHigh));
            if (ImGui.Combo("##targeting", ref current, CachedTargetingNames, CachedTargetingModes.Length))
            {
                _workingTargetingMode = (TargetingMode)current;
            }
            ImGui.PopStyleColor(3);

            DrawHeaderSeparator();
        }

        private void DrawHeaderSeparator()
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetContentRegionAvail().X;
            drawList.AddLine(pos, new Vector2(pos.X + w, pos.Y), ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.GlassBorderAccent)));
            ImGui.Dummy(new Vector2(0, 2));
        }

        private void DrawInventoryPane()
        {
            ImGui.TextColored(TV4(DesignTokens.Primary), "INVENTORY");
            ImGui.Spacing();

            DrawModCategory("BEHAVIOR", ModCategoryFilter.Behavior, DesignTokens.ColorTarget);
            ImGui.Spacing();
            DrawModCategory("ELEMENTAL", ModCategoryFilter.Elemental, DesignTokens.ColorEffect);
            ImGui.Spacing();
            DrawModCategory("EVENTS", ModCategoryFilter.Events, DesignTokens.ColorTrigger);
        }

        private void DrawModCategory(string label, ModCategoryFilter filter, Color accentColor)
        {
            ImGui.TextColored(TV4(accentColor), label);

            float availW = ImGui.GetContentRegionAvail().X;
            float cardW = MathF.Min(80, (availW - DesignTokens.SpaceXs) * 0.5f);
            float cardH = 52;
            float spacing = DesignTokens.SpaceXs;
            int cols = Math.Max(1, (int)((availW + spacing) / (cardW + spacing)));

            int col = 0;
            foreach (var modType in CachedModTypes)
            {
                if (!MatchesFilter(modType, filter)) continue;

                int available = _inventory!.GetAvailable(modType, _tower, _workingSlots);

                if (col > 0)
                    ImGui.SameLine(0, spacing);

                DrawInventoryCard(modType, available, cardW, cardH, accentColor);

                col++;
                if (col >= cols) col = 0;
            }
        }

        private void DrawInventoryCard(ModType modType, int available, float w, float h, Color catColor)
        {
            ImGui.PushID((int)modType + 1000);

            var cursorPos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var modColor = GetModColor(modType);
            bool hasStock = available > 0;
            float alpha = hasStock ? 1f : 0.35f;

            ImGui.PushStyleColor(ImGuiCol.Button, TV4A(DesignTokens.SurfaceContainerHigh, alpha * 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.GlassInnerBgHover, alpha));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.SurfaceContainerHighest, alpha));

            ImGui.Button($"##inv_{modType}", new Vector2(w, h));

            bool isHovered = ImGui.IsItemHovered();
            if (isHovered)
                _hoveredMod = modType;

            if (hasStock && ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
            {
                int payload = (int)modType;
                unsafe
                {
                    ImGui.SetDragDropPayload("MOD_TYPE", (IntPtr)(&payload), sizeof(int));
                }
                _draggedMod = modType;
                _dragActive = true;
                ImGui.EndDragDropSource();
            }

            ImGui.PopStyleColor(3);

            float iconSize = 14;
            var iconPos = new Vector2(cursorPos.X + 6, cursorPos.Y + 6);
            drawList.AddRectFilled(iconPos, new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize),
                ImGui.ColorConvertFloat4ToU32(TV4A(modColor, alpha)), 2f);

            var namePos = new Vector2(cursorPos.X + 6, cursorPos.Y + 24);
            drawList.AddText(namePos, ImGui.ColorConvertFloat4ToU32(TV4A(DesignTokens.OnSurface, alpha)),
                modType.ToString());

            var countText = $"x{available}";
            var countPos = new Vector2(cursorPos.X + 6, cursorPos.Y + h - 16);
            var countColor = available > 0 ? DesignTokens.OnSurfaceVariant : DesignTokens.Error;
            drawList.AddText(countPos, ImGui.ColorConvertFloat4ToU32(TV4A(countColor, alpha)), countText);

            if (isHovered)
            {
                drawList.AddRect(cursorPos, new Vector2(cursorPos.X + w, cursorPos.Y + h),
                    ImGui.ColorConvertFloat4ToU32(TV4(modColor)), 3f, ImDrawFlags.None, 1.5f);
            }

            ImGui.PopID();
        }

        private void DrawSlotChain()
        {
            ImGui.TextColored(TV4(DesignTokens.Primary), "MOD CHAIN");
            ImGui.SameLine();
            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant),
                $"({_workingSlots.Count}/{_tower!.Data.SlotCount})");
            ImGui.Spacing();

            int slotCount = _tower.Data.SlotCount;
            var drawList = ImGui.GetWindowDrawList();

            float availW = ImGui.GetContentRegionAvail().X;
            float connW = 20;
            float totalConn = (slotCount - 1) * connW;
            float slotW = (availW - totalConn) / slotCount;
            slotW = MathF.Max(slotW, 60);
            float slotH = 56;

            var origin = ImGui.GetCursorScreenPos();

            for (int i = 0; i < slotCount; i++)
            {
                float sx = origin.X + i * (slotW + connW);
                float sy = origin.Y;

                if (i > 0)
                {
                    float cx = sx - connW;
                    float midY = sy + slotH * 0.5f;

                    bool hasSynergy = i - 1 < _workingSlots.Count && i < _workingSlots.Count &&
                        SynergyTable.Check(_workingSlots[i - 1].modType, _workingSlots[i].modType).HasValue;
                    var lineColor = hasSynergy ? DesignTokens.Success : DesignTokens.GlassBorderAccent;
                    float thickness = hasSynergy ? 2.5f : 1.5f;

                    drawList.AddLine(new Vector2(cx, midY), new Vector2(cx + connW, midY),
                        ImGui.ColorConvertFloat4ToU32(TV4(lineColor)), thickness);
                    float a = 4;
                    drawList.AddTriangleFilled(
                        new Vector2(cx + connW - a, midY - a),
                        new Vector2(cx + connW - a, midY + a),
                        new Vector2(cx + connW, midY),
                        ImGui.ColorConvertFloat4ToU32(TV4(lineColor)));

                    if (hasSynergy)
                    {
                        var syn = SynergyTable.Check(_workingSlots[i - 1].modType, _workingSlots[i].modType);
                        if (syn.HasValue)
                        {
                            var txt = syn.Value.synergyName;
                            var ts = ImGui.CalcTextSize(txt);
                            drawList.AddText(new Vector2(cx + (connW - ts.X) * 0.5f, midY - ts.Y - 2),
                                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.Success)), txt);
                        }
                    }
                }

                ImGui.SetCursorScreenPos(new Vector2(sx, sy));
                ImGui.PushID(i);

                bool isOccupied = i < _workingSlots.Count;
                if (isOccupied)
                    DrawOccupiedSlot(i, drawList, slotW, slotH);
                else
                    DrawEmptySlot(i, drawList, slotW, slotH);

                ImGui.PopID();
            }

            ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + slotH + 4));
            ImGui.Dummy(new Vector2(availW, 0));
        }

        private void DrawOccupiedSlot(int index, ImDrawListPtr drawList, float slotW, float slotH)
        {
            var slot = _workingSlots[index];
            var modColor = GetModColor(slot.modType);
            bool isEvent = slot.modType.IsEvent();

            ImGui.PushStyleColor(ImGuiCol.Button, TV4A(DesignTokens.SurfaceContainerHigh, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.GlassInnerBgHover, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.SurfaceContainerHighest, 0.95f));

            var cursor = ImGui.GetCursorScreenPos();
            ImGui.Button($"##slot{index}", new Vector2(slotW, slotH));

            bool hovered = ImGui.IsItemHovered();
            if (hovered) _hoveredMod = slot.modType;

            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
            {
                int payload = (int)slot.modType;
                unsafe { ImGui.SetDragDropPayload("MOD_SLOT_REMOVE", (IntPtr)(&payload), sizeof(int)); }
                _draggedMod = slot.modType;
                _dragActive = true;
                ImGui.EndDragDropSource();
                _workingSlots.RemoveAt(index);
                RefreshSynergies();
                ImGui.PopStyleColor(3);
                return;
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payloadPtr = ImGui.AcceptDragDropPayload("MOD_TYPE");
                unsafe
                {
                    if (payloadPtr.NativePtr != null)
                    {
                        int modTypeInt = *(int*)payloadPtr.Data;
                        var modType = (ModType)modTypeInt;
                        if (_workingSlots.Count < _tower!.Data.SlotCount)
                            _workingSlots.Insert(index, new ModSlotData { modType = modType });
                        else
                            _workingSlots[index] = new ModSlotData { modType = modType };
                        RefreshSynergies();
                    }
                }
                ImGui.EndDragDropTarget();
            }

            ImGui.PopStyleColor(3);

            // Top color bar
            drawList.AddRectFilled(cursor, new Vector2(cursor.X + slotW, cursor.Y + 3),
                ImGui.ColorConvertFloat4ToU32(TV4(modColor)));

            if (isEvent)
            {
                drawList.AddRect(cursor, new Vector2(cursor.X + slotW, cursor.Y + slotH),
                    ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.Tertiary)), 3f, ImDrawFlags.None, 1.5f);
            }

            float iconSize = 10;
            drawList.AddRectFilled(
                new Vector2(cursor.X + 6, cursor.Y + 10),
                new Vector2(cursor.X + 6 + iconSize, cursor.Y + 10 + iconSize),
                ImGui.ColorConvertFloat4ToU32(TV4(modColor)), 2f);

            var nameText = slot.modType.ToString().ToUpperInvariant();
            drawList.AddText(new Vector2(cursor.X + 6, cursor.Y + 24),
                ImGui.ColorConvertFloat4ToU32(TV4(modColor)), nameText);

            string category = slot.modType.IsElemental() ? "ELM" : slot.modType.IsEvent() ? "EVT" : "BHV";
            drawList.AddText(new Vector2(cursor.X + 6, cursor.Y + slotH - 14),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OnSurfaceVariant)), category);

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && hovered)
            {
                _workingSlots.RemoveAt(index);
                RefreshSynergies();
            }
        }

        private void DrawEmptySlot(int index, ImDrawListPtr drawList, float slotW, float slotH)
        {
            var cursor = ImGui.GetCursorScreenPos();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.GlassInnerBgHover, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.GlassInnerBgHover, 0.5f));

            ImGui.Button($"##emptyslot{index}", new Vector2(slotW, slotH));

            if (ImGui.BeginDragDropTarget())
            {
                var payloadPtr = ImGui.AcceptDragDropPayload("MOD_TYPE");
                unsafe
                {
                    if (payloadPtr.NativePtr != null)
                    {
                        int modTypeInt = *(int*)payloadPtr.Data;
                        var modType = (ModType)modTypeInt;
                        int insertAt = Math.Min(index, _workingSlots.Count);
                        _workingSlots.Insert(insertAt, new ModSlotData { modType = modType });
                        RefreshSynergies();
                    }
                }
                ImGui.EndDragDropTarget();
            }

            ImGui.PopStyleColor(3);

            float dashLen = 5;
            float gapLen = 3;
            var dashColor = ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OutlineVariant));

            float x = cursor.X;
            while (x < cursor.X + slotW)
            {
                float e = MathF.Min(x + dashLen, cursor.X + slotW);
                drawList.AddLine(new Vector2(x, cursor.Y), new Vector2(e, cursor.Y), dashColor, 1f);
                drawList.AddLine(new Vector2(x, cursor.Y + slotH), new Vector2(e, cursor.Y + slotH), dashColor, 1f);
                x += dashLen + gapLen;
            }
            float y = cursor.Y;
            while (y < cursor.Y + slotH)
            {
                float e = MathF.Min(y + dashLen, cursor.Y + slotH);
                drawList.AddLine(new Vector2(cursor.X, y), new Vector2(cursor.X, e), dashColor, 1f);
                drawList.AddLine(new Vector2(cursor.X + slotW, y), new Vector2(cursor.X + slotW, e), dashColor, 1f);
                y += dashLen + gapLen;
            }

            var text = $"+{index + 1}";
            var textSize = ImGui.CalcTextSize(text);
            drawList.AddText(
                new Vector2(cursor.X + (slotW - textSize.X) * 0.5f, cursor.Y + (slotH - textSize.Y) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OutlineVariant)), text);
        }

        private void DrawInfoArea()
        {
            ImGui.TextColored(TV4(DesignTokens.Primary), "INFO");
            ImGui.Spacing();

            if (_hoveredMod.HasValue)
            {
                var mod = _hoveredMod.Value;
                ImGui.TextColored(TV4(GetModColor(mod)), mod.ToString().ToUpperInvariant());
                string desc = GetModDescription(mod);
                ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), desc);
                string category = mod.IsElemental() ? "ELEMENT" : mod.IsEvent() ? "EVENT" : "BEHAVIOR";
                ImGui.TextColored(TV4(GetModColor(mod)), category);
            }
            else
            {
                ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), "Hover a mod for details");
            }

            ImGui.Spacing();
            ImGui.Spacing();

            if (_activeSynergies.Count > 0)
            {
                ImGui.TextColored(TV4(DesignTokens.Success), "ACTIVE SYNERGIES");
                foreach (var syn in _activeSynergies)
                {
                    ImGui.TextColored(TV4(DesignTokens.Success), $"  {syn}");
                }
                ImGui.Spacing();
            }

            DrawPipelinePreview();
        }

        private void DrawPipelinePreview()
        {
            if (_workingSlots.Count == 0) return;

            ImGui.TextColored(TV4(DesignTokens.OnSurfaceVariant), "PIPELINE");

            var synergies = new List<SynergyEffect>(_activeSynergies);
            try
            {
                var (pipeline, ctx) = PipelineCompiler.Compile(_workingSlots, _tower!.Data.BaseDamage, synergies);
                var tags = pipeline.AccumulatedTags;
                var activeFlags = new List<string>();

                foreach (var flag in CachedModTags)
                {
                    if (flag == ModTags.None) continue;
                    if (tags.HasFlag(flag))
                        activeFlags.Add(flag.ToString());
                }

                if (activeFlags.Count > 0)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(TV4(DesignTokens.OnSurface), string.Join(" > ", activeFlags));
                }
            }
            catch
            {
                ImGui.TextColored(TV4(DesignTokens.Error), "Compile error");
            }
        }

        private void RefreshSynergies()
        {
            _activeSynergies.Clear();
            for (int i = 0; i < _workingSlots.Count - 1; i++)
            {
                var syn = SynergyTable.Check(_workingSlots[i].modType, _workingSlots[i + 1].modType);
                if (syn.HasValue && !_activeSynergies.Contains(syn.Value.effect))
                    _activeSynergies.Add(syn.Value.effect);
            }
        }

        private float ComputeEffectiveDamage()
        {
            float dmg = _tower!.Data.BaseDamage;
            foreach (var s in _workingSlots)
            {
                if (s.modType == ModType.Heavy) dmg *= 1.6f;
                if (s.modType == ModType.Swift) dmg *= 0.7f;
            }
            return dmg;
        }

        private float ComputeEffectiveFireRate()
        {
            float rate = _tower!.Data.FireRate;
            if (_activeSynergies.Contains(SynergyEffect.Machinegun))
                rate *= 2f;
            return rate;
        }

        private void PushPanelStyle()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.03f, 0.04f, 0.06f, 0.92f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0f, 0.8f, 1f, 0.15f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, TV4(DesignTokens.SurfaceContainerLow));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, TV4(DesignTokens.SurfaceContainerHigh));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, TV4A(DesignTokens.SurfaceContainerLow, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, TV4(DesignTokens.OutlineVariant));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(DesignTokens.SpaceLg + 4, DesignTokens.SpaceMd));
        }

        private void PopPanelStyle()
        {
            ImGui.PopStyleVar(4);
            ImGui.PopStyleColor(6);
        }

        private static Color GetModColor(ModType type)
        {
            if (type.IsEvent()) return DesignTokens.ColorTrigger;
            if (type.IsElemental())
            {
                return type switch
                {
                    ModType.Burn => DesignTokens.ElementBurn,
                    ModType.Frost => DesignTokens.ElementFrost,
                    ModType.Shock => DesignTokens.ElementShock,
                    ModType.Void => DesignTokens.ElementVoid,
                    ModType.Leech => DesignTokens.ElementLeech,
                    _ => DesignTokens.ColorEffect,
                };
            }
            return DesignTokens.ColorTarget;
        }

        private static string GetModDescription(ModType type)
        {
            return type switch
            {
                ModType.Homing => "Projectile tracks nearest enemy",
                ModType.Pierce => "Projectile passes through enemies",
                ModType.Bounce => "Projectile bounces between enemies",
                ModType.Split => "Fires multiple projectiles",
                ModType.Heavy => "Increased damage, slower projectile",
                ModType.Swift => "Reduced damage, faster fire rate",
                ModType.Wide => "Increased area of effect",
                ModType.Burn => "Applies damage over time (fire)",
                ModType.Frost => "Slows enemies on hit",
                ModType.Shock => "Chain lightning to nearby enemies",
                ModType.Void => "Reduces enemy defenses",
                ModType.Leech => "Heals objective on hit",
                ModType.OnHit => "Triggers sub-projectile on each hit",
                ModType.OnKill => "Triggers sub-projectile on kill",
                ModType.OnEnd => "Triggers sub-projectile on expiry",
                ModType.OnDelay => "Triggers after a short delay",
                ModType.OnPulse => "Triggers periodically during flight",
                ModType.IfBurning => "Triggers if target is burning",
                ModType.IfFrozen => "Triggers if target is frozen",
                ModType.IfLow => "Triggers if target HP is low",
                ModType.OnOverkill => "Triggers on overkill damage",
                _ => ""
            };
        }

        private enum ModCategoryFilter { Behavior, Elemental, Events }

        private static bool MatchesFilter(ModType type, ModCategoryFilter filter)
        {
            return filter switch
            {
                ModCategoryFilter.Behavior => type.IsTrait() && !type.IsElemental(),
                ModCategoryFilter.Elemental => type.IsElemental(),
                ModCategoryFilter.Events => type.IsEvent(),
                _ => false
            };
        }

        private static Vector4 TV4(Color c) =>
            new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

        private static Vector4 TV4A(Color c, float alpha) =>
            new(c.R / 255f, c.G / 255f, c.B / 255f, alpha);
    }
}
