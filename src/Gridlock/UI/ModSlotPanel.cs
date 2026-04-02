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
        private Tower? _tower;
        private PlayerInventory? _inventory;
        private readonly List<ModSlotData> _workingSlots = new();
        private readonly List<SynergyEffect> _activeSynergies = new();
        private TargetingMode _workingTargetingMode;
        private ModType? _hoveredMod;

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
            float panelW = 640;
            float panelH = MathF.Min(screenH - 40f, 620f);

            ImGui.SetNextWindowPos(new Vector2((screenW - panelW) * 0.5f, (screenH - panelH) * 0.5f), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(panelW, panelH));

            PushPanelStyle();

            bool open = true;
            ImGui.Begin("MOD EDITOR##ModPanel", ref open,
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

            float contentW = ImGui.GetContentRegionAvail().X;
            float leftW = contentW * 0.42f;
            float rightW = contentW - leftW - DesignTokens.SpaceMd;

            ImGui.BeginChild("##LeftPane", new Vector2(leftW, 0), ImGuiChildFlags.None);
            DrawInventoryPane();
            ImGui.EndChild();

            ImGui.SameLine(0, DesignTokens.SpaceMd);

            ImGui.BeginChild("##RightPane", new Vector2(rightW, 0), ImGuiChildFlags.None);
            DrawSlotChain();
            ImGui.Separator();
            ImGui.Spacing();
            DrawInfoArea();
            ImGui.EndChild();

            ImGui.End();
            PopPanelStyle();
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
            var modes = Enum.GetValues<TargetingMode>();
            int current = (int)_workingTargetingMode;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, TV4(DesignTokens.SurfaceContainerHigh));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, TV4(DesignTokens.GlassInnerBgHover));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, TV4(DesignTokens.SurfaceContainerHigh));
            if (ImGui.Combo("##targeting", ref current, Enum.GetNames<TargetingMode>(), modes.Length))
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
            float cardW = 80;
            float cardH = 60;
            float spacing = DesignTokens.SpaceXs;
            int cols = Math.Max(1, (int)((availW + spacing) / (cardW + spacing)));

            int col = 0;
            foreach (var modType in Enum.GetValues<ModType>())
            {
                if (!MatchesFilter(modType, filter)) continue;

                int count = _inventory!.GetCount(modType);
                int usedInSlots = CountInSlots(modType);
                int available = count - usedInSlots;

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
                ImGui.TextColored(TV4(modColor), modType.ToString());
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

            for (int i = 0; i < slotCount; i++)
            {
                if (i > 0)
                    DrawSlotConnector(drawList, i);

                ImGui.PushID(i);

                bool isOccupied = i < _workingSlots.Count;

                if (isOccupied)
                    DrawOccupiedSlot(i, drawList);
                else
                    DrawEmptySlot(i, drawList);

                ImGui.PopID();
            }
        }

        private void DrawSlotConnector(ImDrawListPtr drawList, int slotIndex)
        {
            var cursor = ImGui.GetCursorScreenPos();
            float midX = cursor.X + ImGui.GetContentRegionAvail().X * 0.5f;
            float lineH = 16;

            bool hasSynergy = false;
            if (slotIndex > 0 && slotIndex - 1 < _workingSlots.Count && slotIndex < _workingSlots.Count)
            {
                hasSynergy = SynergyTable.Check(
                    _workingSlots[slotIndex - 1].modType,
                    _workingSlots[slotIndex].modType).HasValue;
            }

            var lineColor = hasSynergy ? DesignTokens.Success : DesignTokens.GlassBorderAccent;
            drawList.AddLine(
                new Vector2(midX, cursor.Y),
                new Vector2(midX, cursor.Y + lineH),
                ImGui.ColorConvertFloat4ToU32(TV4(lineColor)), hasSynergy ? 2.5f : 1.5f);

            float arrowSize = 4;
            drawList.AddTriangleFilled(
                new Vector2(midX - arrowSize, cursor.Y + lineH - arrowSize),
                new Vector2(midX + arrowSize, cursor.Y + lineH - arrowSize),
                new Vector2(midX, cursor.Y + lineH),
                ImGui.ColorConvertFloat4ToU32(TV4(lineColor)));

            if (hasSynergy)
            {
                var synergy = SynergyTable.Check(
                    _workingSlots[slotIndex - 1].modType,
                    _workingSlots[slotIndex].modType);
                if (synergy.HasValue)
                {
                    var text = synergy.Value.synergyName;
                    var textSize = ImGui.CalcTextSize(text);
                    drawList.AddText(
                        new Vector2(midX + arrowSize + 6, cursor.Y + (lineH - textSize.Y) * 0.5f),
                        ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.Success)),
                        text);
                }
            }

            ImGui.Dummy(new Vector2(0, lineH));
        }

        private void DrawOccupiedSlot(int index, ImDrawListPtr drawList)
        {
            var slot = _workingSlots[index];
            var modColor = GetModColor(slot.modType);
            bool isEvent = slot.modType.IsEvent();

            float slotH = 36;
            float availW = ImGui.GetContentRegionAvail().X;

            ImGui.PushStyleColor(ImGuiCol.Button, TV4A(DesignTokens.SurfaceContainerHigh, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.GlassInnerBgHover, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.SurfaceContainerHighest, 0.95f));

            var cursor = ImGui.GetCursorScreenPos();

            ImGui.Button($"##slot{index}", new Vector2(availW, slotH));

            bool hovered = ImGui.IsItemHovered();
            if (hovered)
                _hoveredMod = slot.modType;

            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
            {
                int payload = (int)slot.modType;
                unsafe
                {
                    ImGui.SetDragDropPayload("MOD_SLOT_REMOVE", (IntPtr)(&payload), sizeof(int));
                }
                ImGui.TextColored(TV4(modColor), $"Remove {slot.modType}");
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
                        {
                            _workingSlots.Insert(index, new ModSlotData { modType = modType });
                        }
                        else
                        {
                            _workingSlots[index] = new ModSlotData { modType = modType };
                        }
                        RefreshSynergies();
                    }
                }
                ImGui.EndDragDropTarget();
            }

            ImGui.PopStyleColor(3);

            float borderThickness = isEvent ? 2f : 1.5f;
            var borderColor = isEvent ? DesignTokens.Tertiary : modColor;
            if (isEvent)
            {
                drawList.AddRect(cursor, new Vector2(cursor.X + availW, cursor.Y + slotH),
                    ImGui.ColorConvertFloat4ToU32(TV4(borderColor)), 4f, ImDrawFlags.None, borderThickness);
            }

            float iconSize = 10;
            drawList.AddRectFilled(
                new Vector2(cursor.X + 8, cursor.Y + (slotH - iconSize) * 0.5f),
                new Vector2(cursor.X + 8 + iconSize, cursor.Y + (slotH + iconSize) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(TV4(modColor)), 2f);

            drawList.AddLine(
                new Vector2(cursor.X, cursor.Y),
                new Vector2(cursor.X, cursor.Y + slotH),
                ImGui.ColorConvertFloat4ToU32(TV4(modColor)), 3f);

            var nameText = slot.modType.ToString().ToUpperInvariant();
            drawList.AddText(
                new Vector2(cursor.X + 24, cursor.Y + 4),
                ImGui.ColorConvertFloat4ToU32(TV4(modColor)), nameText);

            string category = slot.modType.IsElemental() ? "ELEMENT"
                : slot.modType.IsEvent() ? "EVENT"
                : "BEHAVIOR";
            drawList.AddText(
                new Vector2(cursor.X + 24, cursor.Y + slotH - 16),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OnSurfaceVariant)), category);

            var indexText = $"{index + 1:D2}";
            var indexSize = ImGui.CalcTextSize(indexText);
            drawList.AddText(
                new Vector2(cursor.X + availW - indexSize.X - 8, cursor.Y + (slotH - indexSize.Y) * 0.5f),
                ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OutlineVariant)), indexText);

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && hovered)
            {
                _workingSlots.RemoveAt(index);
                RefreshSynergies();
            }
        }

        private void DrawEmptySlot(int index, ImDrawListPtr drawList)
        {
            float slotH = 36;
            float availW = ImGui.GetContentRegionAvail().X;

            var cursor = ImGui.GetCursorScreenPos();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TV4A(DesignTokens.GlassInnerBgHover, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, TV4A(DesignTokens.GlassInnerBgHover, 0.5f));

            ImGui.Button($"##emptyslot{index}", new Vector2(availW, slotH));

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

            float dashLen = 6;
            float gapLen = 4;
            var dashColor = ImGui.ColorConvertFloat4ToU32(TV4(DesignTokens.OutlineVariant));

            float x = cursor.X;
            while (x < cursor.X + availW)
            {
                float endX = MathF.Min(x + dashLen, cursor.X + availW);
                drawList.AddLine(new Vector2(x, cursor.Y), new Vector2(endX, cursor.Y), dashColor, 1f);
                drawList.AddLine(new Vector2(x, cursor.Y + slotH), new Vector2(endX, cursor.Y + slotH), dashColor, 1f);
                x += dashLen + gapLen;
            }
            float y = cursor.Y;
            while (y < cursor.Y + slotH)
            {
                float endY = MathF.Min(y + dashLen, cursor.Y + slotH);
                drawList.AddLine(new Vector2(cursor.X, y), new Vector2(cursor.X, endY), dashColor, 1f);
                drawList.AddLine(new Vector2(cursor.X + availW, y), new Vector2(cursor.X + availW, endY), dashColor, 1f);
                y += dashLen + gapLen;
            }

            var text = $"+ SLOT {index + 1}";
            var textSize = ImGui.CalcTextSize(text);
            drawList.AddText(
                new Vector2(cursor.X + (availW - textSize.X) * 0.5f, cursor.Y + (slotH - textSize.Y) * 0.5f),
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

                foreach (ModTags flag in Enum.GetValues<ModTags>())
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

        private int CountInSlots(ModType type)
        {
            int c = 0;
            foreach (var s in _workingSlots)
                if (s.modType == type) c++;
            return c;
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
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.04f, 0.06f, 0.08f, 0.88f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0f, 0.8f, 1f, 0.3f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, TV4(DesignTokens.SurfaceContainerLow));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, TV4(DesignTokens.SurfaceContainerHigh));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, TV4A(DesignTokens.SurfaceContainerLow, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, TV4(DesignTokens.OutlineVariant));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(DesignTokens.SpaceLg, DesignTokens.SpaceMd));
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
