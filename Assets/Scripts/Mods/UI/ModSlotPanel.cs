using System;
using System.Collections.Generic;
using Gridlock.Audio;
using Gridlock.CameraSystem;
using Gridlock.Loot;
using Gridlock.Towers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.Mods.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ModSlotPanel : MonoBehaviour
    {
        [SerializeField] private float focusOrthoSize = 10f;
        [SerializeField] private int maxSlots = 7;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _slotBar;
        private VisualElement _synergyBar;
        private Label _towerNameLabel;
        private Label _statRange;
        private Label _statFireRate;
        private Label _statDamage;
        private DropdownField _targetingDropdown;
        private Controls _controls;
        private bool _isOpen;

        private ModSlotExecutor _executor;
        private Transform _towerTransform;
        private List<ModSlotData> _workingSlots = new();
        private List<ModSlotData> _originalSlots = new();
        private TargetingMode _originalTargetingMode;

        private Button _saveBtn;
        private Button _cancelBtn;

        private Label _infoName;
        private Label _infoDesc;
        private Label _infoCat;

        private VisualElement _dragGhost;
        private bool _isDragging;
        private DragSource _dragSource;
        private ModType _dragModType;
        private int _dragSlotIndex = -1;

        public bool IsOpen => _isOpen;

        private static ModSlotPanel _instance;
        public static ModSlotPanel Instance => _instance;

        private enum DragSource { Inventory, Slot }

        private void Awake()
        {
            _instance = this;
            _controls = new Controls();
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            _uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
            if (_uiDocument.rootVisualElement.parent != null)
                _uiDocument.rootVisualElement.parent.pickingMode = PickingMode.Ignore;

            _root = _uiDocument.rootVisualElement.Q("modslot-root");
            if (_root == null) return;

            _root.style.display = DisplayStyle.None;

            _towerNameLabel = _root.Q<Label>("tower-name");
            _statRange = _root.Q<Label>("stat-range");
            _statFireRate = _root.Q<Label>("stat-fire-rate");
            _statDamage = _root.Q<Label>("stat-damage");
            _targetingDropdown = _root.Q<DropdownField>("targeting-dropdown");
            _slotBar = _root.Q("slot-bar");
            _synergyBar = _root.Q("synergy-bar");
            _infoName = _root.Q<Label>("info-name");
            _infoDesc = _root.Q<Label>("info-desc");
            _infoCat = _root.Q<Label>("info-category");

            if (_targetingDropdown != null)
            {
                var choices = new List<string>();
                foreach (var mode in Enum.GetValues(typeof(TargetingMode)))
                    choices.Add(mode.ToString().ToUpperInvariant());
                _targetingDropdown.choices = choices;
                ResetDropdownStyles(_targetingDropdown);
            }

            _saveBtn = _root.Q<Button>("save-btn");
            if (_saveBtn != null)
                _saveBtn.clicked += OnSave;

            _cancelBtn = _root.Q<Button>("cancel-btn");
            if (_cancelBtn != null)
                _cancelBtn.clicked += OnCancel;

        }

        public void Open(ModSlotExecutor executor, Transform towerTransform)
        {
            _executor = executor;
            _towerTransform = towerTransform;
            _isOpen = true;

            _originalSlots.Clear();
            _workingSlots.Clear();
            foreach (var slot in executor.ModSlots)
            {
                _originalSlots.Add(new ModSlotData { modType = slot.modType });
                _workingSlots.Add(new ModSlotData { modType = slot.modType });
            }
            _originalTargetingMode = executor.TargetingMode;

            if (_root != null)
                _root.style.display = DisplayStyle.Flex;

            PopulateHeader();
            RebuildSlotBar();

            if (_towerTransform != null)
                TopDownCamera.Instance?.FocusOn(_towerTransform.position, focusOrthoSize);

            TopDownCamera.Instance?.SetInputEnabled(false);

            SoundManager.Instance?.PlayUI(SoundType.EditorOpen);

            InventoryPanel.Instance?.OpenForEditing();

            _controls.Player.Disable();
            _controls.UI.Enable();
            _controls.UI.Cancel.performed -= OnCancelPerformed;
            _controls.UI.Cancel.performed += OnCancelPerformed;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            CancelDrag();

            SoundManager.Instance?.PlayUI(SoundType.EditorClose);

            TopDownCamera.Instance?.RestoreFocus();
            TopDownCamera.Instance?.SetInputEnabled(true);

            if (_root != null)
                _root.style.display = DisplayStyle.None;

            _controls.UI.Cancel.performed -= OnCancelPerformed;
            _controls.UI.Disable();
            _controls.Player.Enable();

            InventoryPanel.Instance?.Close();

            _executor = null;
            _towerTransform = null;
        }

        private void PopulateHeader()
        {
            if (_executor == null) return;

            var chassis = _executor.GetComponent<TowerChassis>();
            if (chassis == null) return;

            if (_towerNameLabel != null)
                _towerNameLabel.text = _executor.gameObject.name.ToUpperInvariant();

            if (_statRange != null)
                _statRange.text = chassis.BaseRange.ToString("F0");

            if (_statFireRate != null)
                _statFireRate.text = chassis.FireRate.ToString("F1");

            if (_statDamage != null)
                _statDamage.text = chassis.BaseDamage.ToString("F0");

            if (_targetingDropdown != null)
            {
                _targetingDropdown.UnregisterValueChangedCallback(OnTargetingChanged);
                _targetingDropdown.index = (int)_executor.TargetingMode;
                _targetingDropdown.RegisterValueChangedCallback(OnTargetingChanged);
            }
        }

        private void RebuildSlotBar()
        {
            if (_slotBar == null) return;
            _slotBar.Clear();

            int slotCount = Mathf.Max(_workingSlots.Count + 1, 1);
            slotCount = Mathf.Min(slotCount, maxSlots);

            for (int i = 0; i < slotCount; i++)
            {
                if (i > 0)
                {
                    var connector = new VisualElement();
                    connector.AddToClassList("slot-connector");

                    if (i < _workingSlots.Count && i - 1 >= 0 && i - 1 < _workingSlots.Count)
                    {
                        bool hasSynergy = SynergyTable.Check(
                            _workingSlots[i - 1].modType,
                            _workingSlots[i].modType).HasValue;
                        if (hasSynergy)
                            connector.AddToClassList("slot-connector--synergy");
                    }

                    _slotBar.Add(connector);
                }

                bool isOccupied = i < _workingSlots.Count;

                if (isOccupied)
                {
                    var slotElement = CreateOccupiedSlotElement(i, _workingSlots[i]);
                    _slotBar.Add(slotElement);
                }
                else
                {
                    var emptySlot = CreateEmptySlotElement(i);
                    _slotBar.Add(emptySlot);
                }
            }

            RebuildSynergyBar();
        }

        private VisualElement CreateOccupiedSlotElement(int index, ModSlotData slotData)
        {
            var slot = new VisualElement();
            slot.AddToClassList("mod-slot");
            slot.userData = index;

            Color modColor = ModSlotColors.GetModColor(slotData.modType);
            string displayName = ModSlotColors.GetModDisplayName(slotData.modType);

            if (slotData.modType.IsEvent())
            {
                slot.AddToClassList("mod-slot--event");
                slot.style.borderLeftColor = new StyleColor(modColor);

                var diamond = new Label("\u27D0");
                diamond.AddToClassList("mod-slot__event-diamond");
                diamond.style.color = new StyleColor(modColor);
                slot.Add(diamond);
            }
            else
            {
                slot.AddToClassList("mod-slot--trait");
                slot.style.borderLeftColor = new StyleColor(modColor);
            }

            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("mod-slot__name");
            nameLabel.style.color = new StyleColor(modColor);
            slot.Add(nameLabel);

            string category = slotData.modType.IsElemental() ? "ELEMENT"
                : slotData.modType.IsEvent() ? "EVENT"
                : "BEHAVIOR";
            var typeLabel = new Label(category);
            typeLabel.AddToClassList("mod-slot__type-label");
            slot.Add(typeLabel);

            var indexLabel = new Label((index + 1).ToString("D2"));
            indexLabel.AddToClassList("mod-slot__index");
            slot.Add(indexLabel);

            int capturedIndex = index;
            ModType capturedType = slotData.modType;
            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                StartDrag(DragSource.Slot, capturedType, capturedIndex, evt.position);
                evt.StopPropagation();
            });
            slot.RegisterCallback<PointerEnterEvent>(_ => ShowModInfo(capturedType));
            slot.RegisterCallback<PointerLeaveEvent>(_ => ClearModInfo());

            return slot;
        }

        private VisualElement CreateEmptySlotElement(int index)
        {
            var slot = new VisualElement();
            slot.AddToClassList("mod-slot");
            slot.AddToClassList("mod-slot--empty");
            slot.userData = index;

            var plusLabel = new Label("+");
            plusLabel.AddToClassList("mod-slot__name");
            slot.Add(plusLabel);

            var indexLabel = new Label((index + 1).ToString("D2"));
            indexLabel.AddToClassList("mod-slot__index");
            slot.Add(indexLabel);

            return slot;
        }

        private void RebuildSynergyBar()
        {
            if (_synergyBar == null) return;
            _synergyBar.Clear();

            for (int i = 0; i < _workingSlots.Count; i++)
            {
                if (i > 0)
                {
                    var synergy = SynergyTable.Check(
                        _workingSlots[i - 1].modType,
                        _workingSlots[i].modType);

                    if (synergy.HasValue)
                    {
                        var label = new Label(synergy.Value.synergyName);
                        label.AddToClassList("synergy-label");

                        var capturedSynergy = synergy.Value;
                        label.RegisterCallback<PointerEnterEvent>(_ => ShowSynergyInfo(capturedSynergy));
                        label.RegisterCallback<PointerLeaveEvent>(_ => ClearModInfo());

                        _synergyBar.Add(label);
                    }
                    else
                    {
                        var spacer = new VisualElement();
                        spacer.AddToClassList("synergy-spacer");
                        _synergyBar.Add(spacer);
                    }
                }
                else
                {
                    var spacer = new VisualElement();
                    spacer.AddToClassList("synergy-spacer");
                    _synergyBar.Add(spacer);
                }
            }
        }

        public void StartDragFromInventory(ModType modType)
        {
            if (!_isOpen || _isDragging) return;
            var panelPos = MouseToPanelPos();
            StartDrag(DragSource.Inventory, modType, -1, panelPos);
        }

        public void ShowModInfoExternal(ModType type)
        {
            ShowModInfo(type);
        }

        private void ShowModInfo(ModType type)
        {
            Color color = ModSlotColors.GetModColor(type);

            if (_infoName != null)
            {
                _infoName.text = ModSlotColors.GetModDisplayName(type);
                _infoName.style.color = new StyleColor(color);
            }

            if (_infoDesc != null)
                _infoDesc.text = ModSlotColors.GetModDescription(type);

            if (_infoCat != null)
            {
                _infoCat.text = ModSlotColors.GetModCategory(type);
                _infoCat.style.color = new StyleColor(color);
            }
        }

        private void ShowSynergyInfo(SynergyDef synergy)
        {
            Color color = new Color(0f, 0.93f, 0.99f);

            if (_infoName != null)
            {
                _infoName.text = synergy.synergyName;
                _infoName.style.color = new StyleColor(color);
            }

            if (_infoDesc != null)
                _infoDesc.text = ModSlotColors.GetSynergyDescription(synergy.effect);

            if (_infoCat != null)
            {
                _infoCat.text = "SYNERGY";
                _infoCat.style.color = new StyleColor(color);
            }
        }

        private void ClearModInfo()
        {
            if (_infoName != null) _infoName.text = "---";
            if (_infoDesc != null) _infoDesc.text = "Hover a module";
            if (_infoCat != null) _infoCat.text = "";
        }

        private void Update()
        {
            if (!_isDragging) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            var panelPos = MouseToPanelPos();
            PositionGhost(panelPos);

            if (mouse.leftButton.wasReleasedThisFrame)
                CompleteDrop(panelPos);
        }

        private Vector2 MouseToPanelPos()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return Vector2.zero;

            var screenPos = mouse.position.ReadValue();
            var panel = _uiDocument.rootVisualElement.panel;
            if (panel != null)
            {
                var flipped = new Vector2(screenPos.x, Screen.height - screenPos.y);
                return RuntimePanelUtils.ScreenToPanel(panel, flipped);
            }

            return new Vector2(screenPos.x, Screen.height - screenPos.y);
        }

        private VisualElement _lastHoveredSlot;

        private void StartDrag(DragSource source, ModType modType, int slotIndex, Vector2 pointerPos)
        {
            _isDragging = true;
            _dragSource = source;
            _dragModType = modType;
            _dragSlotIndex = slotIndex;
            _lastHoveredSlot = null;

            if (source == DragSource.Slot && slotIndex >= 0 && slotIndex < _workingSlots.Count)
            {
                _workingSlots.RemoveAt(slotIndex);
                RebuildSlotBar();
            }

            if (source == DragSource.Inventory)
                PlayerInventory.Instance?.RemoveMod(modType);

            _dragGhost = ModTileFactory.Create(modType, "drag-ghost", blur: true);
            _dragGhost.style.position = Position.Absolute;
            _dragGhost.style.scale = new StyleScale(new Scale(Vector3.one * 0.3f));
            _dragGhost.style.opacity = 0f;

            _uiDocument.rootVisualElement.Add(_dragGhost);
            PositionGhost(pointerPos);

            _dragGhost.schedule.Execute(() =>
            {
                if (_dragGhost == null) return;
                _dragGhost.style.scale = new StyleScale(new Scale(Vector3.one * 1.05f));
                _dragGhost.style.opacity = 0.92f;
            }).ExecuteLater(16);
        }

        private void PositionGhost(Vector2 pointerPos)
        {
            if (_dragGhost == null) return;
            _dragGhost.style.left = pointerPos.x - 85;
            _dragGhost.style.top = pointerPos.y - 36;
            UpdateSlotHighlight(pointerPos);
        }

        private void UpdateSlotHighlight(Vector2 panelPos)
        {
            if (_slotBar == null) return;

            var picked = _root.panel?.Pick(panelPos);
            var slotElement = picked != null ? FindAncestorWithClass(picked, "mod-slot") : null;
            if (slotElement != null && !_slotBar.Contains(slotElement))
                slotElement = null;

            if (slotElement == _lastHoveredSlot) return;

            if (_lastHoveredSlot != null)
            {
                _lastHoveredSlot.style.scale = new StyleScale(new Scale(Vector3.one));
                _lastHoveredSlot.style.borderTopColor = StyleKeyword.Null;
                _lastHoveredSlot.style.borderRightColor = StyleKeyword.Null;
                _lastHoveredSlot.style.borderBottomColor = StyleKeyword.Null;
            }

            _lastHoveredSlot = slotElement;

            if (_lastHoveredSlot != null)
            {
                _lastHoveredSlot.style.scale = new StyleScale(new Scale(Vector3.one * 1.15f));
                var modColor = ModSlotColors.GetModColor(_dragModType);
                _lastHoveredSlot.style.borderTopColor = new StyleColor(modColor);
                _lastHoveredSlot.style.borderRightColor = new StyleColor(modColor);
                _lastHoveredSlot.style.borderBottomColor = new StyleColor(modColor);
            }
        }

        private void CompleteDrop(Vector2 panelPos)
        {
            var dropTarget = FindDropTarget(panelPos);
            bool placed = dropTarget.type == DropZone.EmptySlot || dropTarget.type == DropZone.OccupiedSlot;
            HandleDrop(dropTarget);

            if (placed)
                AnimateGhostPlace();
            else
                AnimateGhostDrop();

            _isDragging = false;
        }

        private void AnimateGhostPlace()
        {
            if (_dragGhost == null) return;
            var ghost = _dragGhost;
            _dragGhost = null;

            ghost.style.scale = new StyleScale(new Scale(Vector3.one * 0.6f));
            ghost.style.opacity = 0f;
            ghost.schedule.Execute(() => ghost.RemoveFromHierarchy()).ExecuteLater(150);

            CleanupHighlight();
            ShakeSlotBar();
        }

        private void ShakeSlotBar()
        {
            if (_slotBar == null) return;

            int i = 0;
            foreach (var child in _slotBar.Children())
            {
                if (!child.ClassListContains("mod-slot")) { i++; continue; }

                int delay = i * 25;
                var slot = child;

                float dir = (i % 2 == 0) ? -3f : 3f;

                slot.schedule.Execute(() =>
                {
                    slot.style.scale = new StyleScale(new Scale(Vector3.one * 1.15f));
                    slot.style.translate = new StyleTranslate(new Translate(0, new Length(dir, LengthUnit.Pixel)));
                }).ExecuteLater(delay);

                slot.schedule.Execute(() =>
                {
                    slot.style.scale = new StyleScale(new Scale(Vector3.one * 1.08f));
                    slot.style.translate = new StyleTranslate(new Translate(0, new Length(-dir * 0.5f, LengthUnit.Pixel)));
                }).ExecuteLater(delay + 80);

                slot.schedule.Execute(() =>
                {
                    slot.style.scale = new StyleScale(new Scale(Vector3.one));
                    slot.style.translate = new StyleTranslate(new Translate(0, 0));
                }).ExecuteLater(delay + 160);

                i++;
            }

            _slotBar.style.translate = new StyleTranslate(new Translate(new Length(-2, LengthUnit.Pixel), 0));
            _slotBar.schedule.Execute(() =>
                _slotBar.style.translate = new StyleTranslate(new Translate(new Length(3, LengthUnit.Pixel), 0))
            ).ExecuteLater(30);
            _slotBar.schedule.Execute(() =>
                _slotBar.style.translate = new StyleTranslate(new Translate(new Length(-1, LengthUnit.Pixel), 0))
            ).ExecuteLater(70);
            _slotBar.schedule.Execute(() =>
                _slotBar.style.translate = new StyleTranslate(new Translate(0, 0))
            ).ExecuteLater(120);

            if (_synergyBar != null)
            {
                _synergyBar.style.opacity = 0.3f;
                _synergyBar.schedule.Execute(() => _synergyBar.style.opacity = 1f).ExecuteLater(150);
            }
        }

        private void AnimateGhostDrop()
        {
            if (_dragGhost == null) return;
            var ghost = _dragGhost;
            _dragGhost = null;

            ghost.style.scale = new StyleScale(new Scale(Vector3.one * 0.2f));
            ghost.style.opacity = 0f;
            ghost.style.rotate = new StyleRotate(new Rotate(new Angle(8f, AngleUnit.Degree)));

            ghost.schedule.Execute(() => ghost.RemoveFromHierarchy()).ExecuteLater(150);

            CleanupHighlight();
        }

        private DropResult FindDropTarget(Vector2 panelPos)
        {
            if (_slotBar != null)
            {
                var picked = _root.panel?.Pick(panelPos);
                if (picked != null)
                {
                    var slotElement = FindAncestorWithClass(picked, "mod-slot");
                    if (slotElement != null && _slotBar.Contains(slotElement))
                    {
                        int index = slotElement.userData is int idx ? idx : -1;
                        bool isEmpty = slotElement.ClassListContains("mod-slot--empty");
                        return new DropResult { type = isEmpty ? DropZone.EmptySlot : DropZone.OccupiedSlot, slotIndex = index };
                    }
                }
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && InventoryPanel.Instance != null && InventoryPanel.Instance.IsOpen)
            {
                if (InventoryPanel.Instance.IsScreenPointOver(mouse.position.ReadValue()))
                    return new DropResult { type = DropZone.Inventory };
            }

            return new DropResult { type = DropZone.None };
        }

        private VisualElement FindAncestorWithClass(VisualElement element, string className)
        {
            var current = element;
            while (current != null)
            {
                if (current.ClassListContains(className))
                    return current;
                current = current.parent;
            }
            return null;
        }

        private void HandleDrop(DropResult dropResult)
        {
            switch (dropResult.type)
            {
                case DropZone.EmptySlot:
                    InsertModAtIndex(dropResult.slotIndex);
                    break;

                case DropZone.OccupiedSlot:
                    SwapOrInsertAtSlot(dropResult.slotIndex);
                    break;

                case DropZone.Inventory:
                case DropZone.None:
                    PlayerInventory.Instance?.AddMod(_dragModType);
                    break;
            }

            RebuildSlotBar();
        }

        private void InsertModAtIndex(int index)
        {
            int insertAt = Mathf.Clamp(index, 0, _workingSlots.Count);

            if (_workingSlots.Count >= maxSlots) return;

            _workingSlots.Insert(insertAt, new ModSlotData { modType = _dragModType });
        }

        private void SwapOrInsertAtSlot(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _workingSlots.Count)
            {
                InsertModAtIndex(targetIndex);
                return;
            }

            if (_dragSource == DragSource.Slot)
            {
                // Slot was already removed during StartDrag, just insert at target
                int insertAt = Mathf.Clamp(targetIndex, 0, _workingSlots.Count);
                _workingSlots.Insert(insertAt, new ModSlotData { modType = _dragModType });
            }
            else
            {
                // From inventory onto occupied slot: insert before
                if (_workingSlots.Count >= maxSlots) return;
                _workingSlots.Insert(targetIndex, new ModSlotData { modType = _dragModType });
            }
        }

        private void RestoreDraggedSlotMod()
        {
            int insertAt = Mathf.Clamp(_dragSlotIndex, 0, _workingSlots.Count);
            _workingSlots.Insert(insertAt, new ModSlotData { modType = _dragModType });
        }

        private void CancelDrag()
        {
            if (!_isDragging) return;

            if (_dragSource == DragSource.Slot)
                RestoreDraggedSlotMod();
            else if (_dragSource == DragSource.Inventory)
                PlayerInventory.Instance?.AddMod(_dragModType);

            CleanupGhost();
            _isDragging = false;
            RebuildSlotBar();
        }

        private void CleanupHighlight()
        {
            if (_lastHoveredSlot != null)
            {
                _lastHoveredSlot.style.scale = new StyleScale(new Scale(Vector3.one));
                _lastHoveredSlot.style.borderTopColor = StyleKeyword.Null;
                _lastHoveredSlot.style.borderRightColor = StyleKeyword.Null;
                _lastHoveredSlot.style.borderBottomColor = StyleKeyword.Null;
                _lastHoveredSlot = null;
            }
        }

        private void CleanupGhost()
        {
            CleanupHighlight();

            if (_dragGhost != null)
            {
                _dragGhost.RemoveFromHierarchy();
                _dragGhost = null;
            }
        }

        private static void ResetDropdownStyles(DropdownField dropdown)
        {
            var s = dropdown.style;
            s.marginTop = s.marginBottom = s.marginLeft = s.marginRight = 0;
            s.paddingTop = s.paddingBottom = s.paddingLeft = s.paddingRight = 0;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 0;
            s.minHeight = StyleKeyword.Auto;
            s.minWidth = StyleKeyword.Auto;
            s.backgroundColor = Color.clear;

            var input = dropdown.Q(className: "unity-base-popup-field__input");
            if (input != null)
            {
                var si = input.style;
                si.marginTop = si.marginBottom = si.marginLeft = si.marginRight = 0;
                si.paddingTop = si.paddingBottom = si.paddingLeft = si.paddingRight = 0;
                si.borderTopWidth = si.borderBottomWidth = si.borderLeftWidth = si.borderRightWidth = 0;
                si.borderTopLeftRadius = si.borderTopRightRadius = 0;
                si.borderBottomLeftRadius = si.borderBottomRightRadius = 0;
                si.minHeight = StyleKeyword.Auto;
                si.minWidth = StyleKeyword.Auto;
                si.backgroundColor = Color.clear;
            }
        }

        private void OnTargetingChanged(ChangeEvent<string> evt)
        {
            if (_targetingDropdown == null || _executor == null) return;
        }

        private void OnSave()
        {
            if (_executor != null)
            {
                _executor.SetSlots(_workingSlots);

                if (_targetingDropdown != null && Enum.TryParse<TargetingMode>(
                        _targetingDropdown.value, true, out var mode))
                    _executor.TargetingMode = mode;
            }
            Close();
        }

        private void OnCancel()
        {
            RestoreInventoryOnCancel();
            if (_executor != null)
                _executor.TargetingMode = _originalTargetingMode;
            Close();
        }

        private void RestoreInventoryOnCancel()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            var originalCounts = new Dictionary<ModType, int>();
            foreach (var slot in _originalSlots)
            {
                originalCounts.TryGetValue(slot.modType, out int c);
                originalCounts[slot.modType] = c + 1;
            }

            var workingCounts = new Dictionary<ModType, int>();
            foreach (var slot in _workingSlots)
            {
                workingCounts.TryGetValue(slot.modType, out int c);
                workingCounts[slot.modType] = c + 1;
            }

            var allTypes = new HashSet<ModType>();
            foreach (var k in originalCounts.Keys) allTypes.Add(k);
            foreach (var k in workingCounts.Keys) allTypes.Add(k);

            foreach (var type in allTypes)
            {
                originalCounts.TryGetValue(type, out int origCount);
                workingCounts.TryGetValue(type, out int workCount);
                int delta = workCount - origCount;

                if (delta > 0)
                    inventory.AddMod(type, delta);
                else if (delta < 0)
                    inventory.RemoveMod(type, -delta);
            }
        }

        private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            OnCancel();
        }

        private void OnDisable()
        {
            if (_saveBtn != null)
                _saveBtn.clicked -= OnSave;

            if (_cancelBtn != null)
                _cancelBtn.clicked -= OnCancel;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _controls?.Dispose();
        }

        private enum DropZone { None, EmptySlot, OccupiedSlot, Inventory }

        private struct DropResult
        {
            public DropZone type;
            public int slotIndex;
        }
    }
}
