using System;
using System.Collections.Generic;
using Gridlock.Audio;
using Gridlock.CameraSystem;
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
        private VisualElement _inventoryGrid;
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
            _inventoryGrid = _root.Q("inventory-grid");
            _infoName = _root.Q<Label>("info-name");
            _infoDesc = _root.Q<Label>("info-desc");
            _infoCat = _root.Q<Label>("info-category");

            if (_targetingDropdown != null)
            {
                var choices = new List<string>();
                foreach (var mode in Enum.GetValues(typeof(TargetingMode)))
                    choices.Add(mode.ToString().ToUpperInvariant());
                _targetingDropdown.choices = choices;
            }

            var saveBtn = _root.Q<Button>("save-btn");
            if (saveBtn != null)
                saveBtn.clicked += OnSave;

            var cancelBtn = _root.Q<Button>("cancel-btn");
            if (cancelBtn != null)
                cancelBtn.clicked += OnCancel;

            _uiDocument.rootVisualElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _uiDocument.rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
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
            PopulateInventory();

            if (_towerTransform != null)
                TopDownCamera.Instance?.FocusOn(_towerTransform.position, focusOrthoSize);

            TopDownCamera.Instance?.SetInputEnabled(false);

            SoundManager.Instance?.PlayUI(SoundType.EditorOpen);

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

        private void PopulateInventory()
        {
            if (_inventoryGrid == null) return;
            _inventoryGrid.Clear();

            foreach (ModType modType in Enum.GetValues(typeof(ModType)))
            {
                var item = new VisualElement();
                item.AddToClassList("inventory-item");

                Color modColor = ModSlotColors.GetModColor(modType);
                item.style.borderLeftColor = new StyleColor(modColor);
                item.style.borderRightColor = new StyleColor(modColor);
                item.style.borderTopColor = new StyleColor(modColor);
                item.style.borderBottomColor = new StyleColor(modColor);

                var nameLabel = new Label(ModSlotColors.GetModDisplayName(modType));
                nameLabel.AddToClassList("inventory-item__name");
                nameLabel.style.color = new StyleColor(modColor);
                item.Add(nameLabel);

                var categoryLabel = new Label(ModSlotColors.GetModCategory(modType));
                categoryLabel.AddToClassList("inventory-item__count");
                item.Add(categoryLabel);

                ModType capturedType = modType;
                item.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    StartDrag(DragSource.Inventory, capturedType, -1, evt.position);
                    evt.StopPropagation();
                });

                item.RegisterCallback<PointerEnterEvent>(_ => ShowModInfo(capturedType));
                item.RegisterCallback<PointerLeaveEvent>(_ => ClearModInfo());

                _inventoryGrid.Add(item);
            }
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

        private void StartDrag(DragSource source, ModType modType, int slotIndex, Vector2 pointerPos)
        {
            _isDragging = true;
            _dragSource = source;
            _dragModType = modType;
            _dragSlotIndex = slotIndex;

            if (source == DragSource.Slot && slotIndex >= 0 && slotIndex < _workingSlots.Count)
            {
                _workingSlots.RemoveAt(slotIndex);
                RebuildSlotBar();
            }

            _dragGhost = new VisualElement();
            _dragGhost.AddToClassList("drag-ghost");
            _dragGhost.pickingMode = PickingMode.Ignore;
            _dragGhost.style.position = Position.Absolute;

            Color color = ModSlotColors.GetModColor(modType);
            _dragGhost.style.borderLeftColor = new StyleColor(color);

            var label = new Label(ModSlotColors.GetModDisplayName(modType));
            label.AddToClassList("drag-ghost__label");
            label.style.color = new StyleColor(color);
            label.pickingMode = PickingMode.Ignore;
            _dragGhost.Add(label);

            _uiDocument.rootVisualElement.Add(_dragGhost);
            PositionGhost(pointerPos);

            _uiDocument.rootVisualElement.CapturePointer(PointerId.mousePointerId);
        }

        private void PositionGhost(Vector2 pointerPos)
        {
            if (_dragGhost == null) return;
            _dragGhost.style.left = pointerPos.x - 40;
            _dragGhost.style.top = pointerPos.y - 15;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging) return;
            PositionGhost(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;
            if (evt.button != 0) return;

            _uiDocument.rootVisualElement.ReleasePointer(PointerId.mousePointerId);

            var localPos = _root.WorldToLocal(evt.position);
            var dropTarget = FindDropTarget(localPos);
            HandleDrop(dropTarget);

            CleanupGhost();
            _isDragging = false;
        }

        private DropResult FindDropTarget(Vector2 localPos)
        {
            if (_slotBar == null) return new DropResult { type = DropZone.None };

            var panelPos = _root.LocalToWorld(localPos);
            var picked = _root.panel.Pick(panelPos);
            if (picked == null) return new DropResult { type = DropZone.None };

            var slotElement = FindAncestorWithClass(picked, "mod-slot");
            if (slotElement != null && _slotBar.Contains(slotElement))
            {
                int index = slotElement.userData is int idx ? idx : -1;
                bool isEmpty = slotElement.ClassListContains("mod-slot--empty");
                return new DropResult { type = isEmpty ? DropZone.EmptySlot : DropZone.OccupiedSlot, slotIndex = index };
            }

            var inventoryElement = FindAncestorWithClass(picked, "inventory-item");
            if (inventoryElement != null)
                return new DropResult { type = DropZone.Inventory };

            if (_inventoryGrid != null && (_inventoryGrid.Contains(picked) || _inventoryGrid == picked))
                return new DropResult { type = DropZone.Inventory };

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
                    // Mod returned to inventory (already removed if from slot)
                    break;

                case DropZone.None:
                    if (_dragSource == DragSource.Slot)
                    {
                        // Dropped outside: restore the removed slot mod
                        RestoreDraggedSlotMod();
                    }
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

            CleanupGhost();
            _isDragging = false;
            RebuildSlotBar();
        }

        private void CleanupGhost()
        {
            if (_dragGhost != null)
            {
                _dragGhost.RemoveFromHierarchy();
                _dragGhost = null;
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
            if (_executor != null)
                _executor.TargetingMode = _originalTargetingMode;
            Close();
        }

        private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            OnCancel();
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
