#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace AIWE.AI
{
    public class ThreatScenarioWindow : EditorWindow
    {
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.EntityIdToObject(EntityId.FromULong((ulong)instanceID));
            if (obj is ThreatCalculatorConfig config)
            {
                Open(config);
                return true;
            }
            return false;
        }

        [Serializable]
        private class TestEntity
        {
            public string name = "Player 1";
            public EntityType type = EntityType.Player;
            public Vector2 position;
            public float recentDPS;
            public bool hasLineOfSight = true;
            public int othersTargeting;

            public float lastScore;
            public float lastDistFactor;
            public float lastLosFactor;
            public float lastDpsFactor;
            public float lastCrowdFactor;
            public bool isSelected;
        }

        private enum EntityType { Player, Tower }

        private ThreatCalculatorConfig _config;
        private readonly List<TestEntity> _entities = new();
        private Vector2 _enemyPosition = Vector2.zero;
        private float _detectionRadius = 12f;

        private TestEntity _draggedEntity;
        private bool _draggingEnemy;
        private int _selectedIndex = -1;
        private Vector2 _scrollPos;

        private const float WORLD_SIZE = 30f;
        private const float ENTITY_RADIUS = 12f;
        private const float ENEMY_RADIUS = 14f;

        // Styles (cached)
        private GUIStyle _headerStyle;
        private GUIStyle _entityLabelStyle;
        private GUIStyle _scoreLabelStyle;
        private GUIStyle _aggroStyle;
        private GUIStyle _sectionStyle;
        private bool _stylesCached;

        public static void Open(ThreatCalculatorConfig config)
        {
            var window = GetWindow<ThreatScenarioWindow>("Threat Scenario Tester");
            window._config = config;
            window.minSize = new Vector2(700, 500);
            config.OnConfigChanged -= window.OnConfigChanged;
            config.OnConfigChanged += window.OnConfigChanged;
            window.Recalc();
            window.Show();
        }

        private void OnConfigChanged()
        {
            Recalc();
            Repaint();
        }

        private void OnDestroy()
        {
            if (_config != null)
                _config.OnConfigChanged -= OnConfigChanged;
        }

        private void CacheStyles()
        {
            if (_stylesCached) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            _entityLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            _scoreLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            _aggroStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            _sectionStyle = new GUIStyle(EditorStyles.helpBox);

            _stylesCached = true;
        }

        private void OnGUI()
        {
            CacheStyles();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("No ThreatCalculatorConfig assigned. Open this window from the config asset.", MessageType.Warning);
                _config = (ThreatCalculatorConfig)EditorGUILayout.ObjectField("Config", _config, typeof(ThreatCalculatorConfig), false);
                if (_config != null)
                {
                    _config.OnConfigChanged -= OnConfigChanged;
                    _config.OnConfigChanged += OnConfigChanged;
                    Recalc();
                }
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            // Left: map
            float mapSize = Mathf.Min(position.width - 280, position.height - 120);
            mapSize = Mathf.Max(mapSize, 300);
            DrawMap(mapSize);

            GUILayout.Space(8);

            // Right: panels
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            DrawConfigSummary();
            EditorGUILayout.Space(4);
            DrawSelectedEntityPanel();
            EditorGUILayout.Space(4);
            DrawScoreTable();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────
        //  TOOLBAR
        // ─────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("+ Player", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                int count = _entities.Count(e => e.type == EntityType.Player) + 1;
                _entities.Add(new TestEntity
                {
                    name = $"Player {count}",
                    type = EntityType.Player,
                    position = new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-8f, 8f)),
                    hasLineOfSight = true
                });
                Recalc();
            }

            if (GUILayout.Button("+ Tower", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                int count = _entities.Count(e => e.type == EntityType.Tower) + 1;
                _entities.Add(new TestEntity
                {
                    name = $"Tower {count}",
                    type = EntityType.Tower,
                    position = new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-8f, 8f)),
                    hasLineOfSight = true
                });
                Recalc();
            }

            GUILayout.Space(8);

            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _entities.Clear();
                _selectedIndex = -1;
                Recalc();
            }

            if (GUILayout.Button("Reset Enemy", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _enemyPosition = Vector2.zero;
                Recalc();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Detection Radius", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            _detectionRadius = EditorGUILayout.Slider(_detectionRadius, 1f, 25f, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck()) Recalc();

            GUILayout.Space(4);

            // Config reference
            EditorGUI.BeginChangeCheck();
            _config = (ThreatCalculatorConfig)EditorGUILayout.ObjectField(_config, typeof(ThreatCalculatorConfig), false, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck() && _config != null)
            {
                _config.OnConfigChanged -= OnConfigChanged;
                _config.OnConfigChanged += OnConfigChanged;
                Recalc();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────
        //  2D MAP
        // ─────────────────────────────────────────────

        private void DrawMap(float size)
        {
            var mapRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

            // Background
            EditorGUI.DrawRect(mapRect, new Color(0.1f, 0.1f, 0.13f));

            // Grid
            DrawGrid(mapRect);

            // Detection radius circle
            Vector2 enemyScreen = WorldToScreen(_enemyPosition, mapRect);
            float radiusPx = (_detectionRadius / WORLD_SIZE) * mapRect.width;
            DrawCircleOutline(enemyScreen, radiusPx, new Color(1f, 0.5f, 0f, 0.15f), 2f);
            // Fill
            DrawFilledCircle(enemyScreen, radiusPx, new Color(1f, 0.5f, 0f, 0.03f));

            // Threat lines
            foreach (var entity in _entities)
            {
                float dist = Vector2.Distance(_enemyPosition, entity.position);
                if (dist > _detectionRadius) continue;

                Vector2 entityScreen = WorldToScreen(entity.position, mapRect);
                float t = Mathf.Clamp01(entity.lastScore / Mathf.Max(_config.aggroThreshold * 2f, 0.01f));
                Color lineColor = Color.Lerp(
                    new Color(0.2f, 0.7f, 0.2f, 0.3f),
                    new Color(1f, 0.15f, 0.05f, 0.6f), t);
                float lineThick = 1.5f;

                if (entity.isSelected)
                {
                    lineColor = new Color(1f, 0.1f, 0.05f, 0.85f);
                    lineThick = 3f;
                }

                DrawLine(enemyScreen, entityScreen, lineColor, lineThick);
            }

            // Entities
            for (int i = 0; i < _entities.Count; i++)
            {
                var entity = _entities[i];
                Vector2 pos = WorldToScreen(entity.position, mapRect);
                float dist = Vector2.Distance(_enemyPosition, entity.position);
                bool inRange = dist <= _detectionRadius;

                Color col;
                if (entity.type == EntityType.Player)
                    col = inRange ? new Color(0.3f, 0.55f, 1f) : new Color(0.15f, 0.25f, 0.45f);
                else
                    col = inRange ? new Color(1f, 0.8f, 0f) : new Color(0.45f, 0.35f, 0f);

                // Target highlight
                if (entity.isSelected)
                {
                    DrawFilledCircle(pos, ENTITY_RADIUS + 5f, new Color(1f, 0.1f, 0.05f, 0.35f));
                    DrawCircleOutline(pos, ENTITY_RADIUS + 5f, new Color(1f, 0.15f, 0.05f, 0.8f), 2f);
                }

                // Selection ring
                if (i == _selectedIndex)
                    DrawCircleOutline(pos, ENTITY_RADIUS + 2f, new Color(1f, 1f, 1f, 0.7f), 2f);

                DrawFilledCircle(pos, ENTITY_RADIUS, col);

                // Label
                string icon = entity.type == EntityType.Player ? "P" : "T";
                _entityLabelStyle.fontSize = 10;
                GUI.Label(new Rect(pos.x - 8, pos.y - 7, 16, 14), icon, _entityLabelStyle);

                // Score
                if (inRange && entity.lastScore > 0f)
                {
                    _scoreLabelStyle.normal.textColor = entity.isSelected
                        ? new Color(1f, 0.25f, 0.15f)
                        : new Color(0.85f, 0.85f, 0.85f);
                    _scoreLabelStyle.fontStyle = entity.isSelected ? FontStyle.Bold : FontStyle.Normal;
                    GUI.Label(new Rect(pos.x - 30, pos.y - ENTITY_RADIUS - 18, 60, 16),
                        $"{entity.lastScore:F2}", _scoreLabelStyle);
                }

                // Name
                var nameStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = inRange ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f) }
                };
                GUI.Label(new Rect(pos.x - 35, pos.y + ENTITY_RADIUS + 2, 70, 14), entity.name, nameStyle);

                // LoS blocked
                if (!entity.hasLineOfSight && inRange)
                {
                    var losStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 8,
                        normal = { textColor = new Color(1f, 0.5f, 0f, 0.9f) }
                    };
                    GUI.Label(new Rect(pos.x - 20, pos.y + ENTITY_RADIUS + 14, 40, 12), "NO LoS", losStyle);
                }
            }

            // Enemy
            DrawFilledCircle(enemyScreen, ENEMY_RADIUS, new Color(0.9f, 0.35f, 0f));
            DrawFilledCircle(enemyScreen, ENEMY_RADIUS - 4f, new Color(0.7f, 0.15f, 0f));
            _entityLabelStyle.fontSize = 11;
            GUI.Label(new Rect(enemyScreen.x - 8, enemyScreen.y - 8, 16, 16), "E", _entityLabelStyle);

            // Aggro status bar at bottom
            var statusRect = new Rect(mapRect.x, mapRect.yMax - 26, mapRect.width, 24);
            EditorGUI.DrawRect(statusRect, new Color(0f, 0f, 0f, 0.5f));

            var best = _entities.FirstOrDefault(e => e.isSelected);
            string status = best != null
                ? $"  AGGRO → {best.name}  (score: {best.lastScore:F3})"
                : $"  NO AGGRO  (best < threshold {_config.aggroThreshold:F2})";
            _aggroStyle.normal.textColor = best != null
                ? new Color(1f, 0.2f, 0.1f)
                : new Color(0.5f, 0.5f, 0.5f);
            _aggroStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(statusRect, status, _aggroStyle);

            // Legend (top-right)
            var legendRect = new Rect(mapRect.xMax - 115, mapRect.y + 5, 110, 52);
            EditorGUI.DrawRect(legendRect, new Color(0f, 0f, 0f, 0.4f));
            var legendStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9 };
            legendStyle.normal.textColor = new Color(0.3f, 0.55f, 1f);
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 2, 100, 14), "● Player", legendStyle);
            legendStyle.normal.textColor = new Color(1f, 0.8f, 0f);
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 14, 100, 14), "● Tower", legendStyle);
            legendStyle.normal.textColor = new Color(0.9f, 0.35f, 0f);
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 26, 100, 14), "● Enemy", legendStyle);
            legendStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 38, 100, 14), "Drag to move", legendStyle);

            HandleInput(mapRect);
        }

        private void DrawGrid(Rect mapRect)
        {
            int gridLines = 12;
            float cellSize = mapRect.width / gridLines;
            var gridColor = new Color(1f, 1f, 1f, 0.04f);
            var axisColor = new Color(1f, 1f, 1f, 0.08f);

            for (int i = 0; i <= gridLines; i++)
            {
                float x = mapRect.x + i * cellSize;
                float y = mapRect.y + i * cellSize;
                var c = (i == gridLines / 2) ? axisColor : gridColor;
                EditorGUI.DrawRect(new Rect(x, mapRect.y, 1, mapRect.height), c);
                EditorGUI.DrawRect(new Rect(mapRect.x, y, mapRect.width, 1), c);
            }
        }

        // ─────────────────────────────────────────────
        //  CONFIG SUMMARY (right panel)
        // ─────────────────────────────────────────────

        private void DrawConfigSummary()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("Config Weights", _headerStyle);
            EditorGUILayout.Space(2);

            DrawWeightBar("Distance", _config.distanceWeight, new Color(0.3f, 0.7f, 1f));
            DrawWeightBar("Line of Sight", _config.lineOfSightWeight, new Color(0.9f, 0.9f, 0.3f));
            DrawWeightBar("DPS", _config.dpsWeight, new Color(1f, 0.3f, 0.2f));
            DrawWeightBar("Crowd", _config.crowdWeight, new Color(0.4f, 0.9f, 0.4f));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"Aggro Threshold: {_config.aggroThreshold:F2}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Max DPS Ref: {_config.maxDPSReference:F0}  |  Max Crowd: {_config.maxCrowdCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawWeightBar(string label, float value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(85));
            var barRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
            float fill = Mathf.Clamp01(value / 2f) * barRect.width;
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, fill, barRect.height), color * 0.6f);
            var valStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };
            GUI.Label(barRect, $"{value:F2} ", valStyle);
            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────
        //  SELECTED ENTITY PANEL
        // ─────────────────────────────────────────────

        private void DrawSelectedEntityPanel()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _entities.Count)
            {
                EditorGUILayout.BeginVertical(_sectionStyle);
                EditorGUILayout.LabelField("Select an entity on the map", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            var entity = _entities[_selectedIndex];

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.BeginHorizontal();
            Color hdrCol = entity.type == EntityType.Player
                ? new Color(0.3f, 0.55f, 1f) : new Color(1f, 0.8f, 0f);
            GUI.color = hdrCol;
            EditorGUILayout.LabelField($"  {entity.name}", _headerStyle);
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                _entities.RemoveAt(_selectedIndex);
                _selectedIndex = -1;
                Recalc();
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            entity.name = EditorGUILayout.TextField("Name", entity.name);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position", GUILayout.Width(EditorGUIUtility.labelWidth));
            entity.position.x = EditorGUILayout.FloatField(entity.position.x);
            entity.position.y = EditorGUILayout.FloatField(entity.position.y);
            EditorGUILayout.EndHorizontal();

            entity.recentDPS = EditorGUILayout.Slider("Recent DPS", entity.recentDPS, 0f, _config.maxDPSReference * 2f);
            entity.hasLineOfSight = EditorGUILayout.Toggle("Line of Sight", entity.hasLineOfSight);
            entity.othersTargeting = EditorGUILayout.IntSlider("Others Targeting", entity.othersTargeting, 0, _config.maxCrowdCount * 2);

            if (EditorGUI.EndChangeCheck()) Recalc();

            // Score breakdown for this entity
            float dist = Vector2.Distance(_enemyPosition, entity.position);
            if (dist <= _detectionRadius)
            {
                EditorGUILayout.Space(4);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.3f, 0.3f, 0.3f));
                EditorGUILayout.Space(2);

                DrawFactorDetail("Distance", entity.lastDistFactor, _config.distanceWeight, new Color(0.3f, 0.7f, 1f));
                DrawFactorDetail("LoS", entity.lastLosFactor, _config.lineOfSightWeight, new Color(0.9f, 0.9f, 0.3f));
                DrawFactorDetail("DPS", entity.lastDpsFactor, _config.dpsWeight, new Color(1f, 0.3f, 0.2f));
                DrawFactorDetail("Crowd", entity.lastCrowdFactor, _config.crowdWeight, new Color(0.4f, 0.9f, 0.4f));

                EditorGUILayout.Space(2);
                var totalStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = entity.isSelected ? new Color(1f, 0.2f, 0.1f) : Color.white }
                };
                EditorGUILayout.LabelField($"Total Score: {entity.lastScore:F3}  {(entity.isSelected ? "← TARGET" : "")}", totalStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFactorDetail(string label, float factor, float weight, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(55));

            var barRect = GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, new Color(0.12f, 0.12f, 0.12f));
            float fill = Mathf.Clamp01(factor) * barRect.width;
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, fill, barRect.height), color * 0.5f);

            EditorGUILayout.LabelField($"{factor:F2} × {weight:F1} = {factor * weight:F2}",
                EditorStyles.miniLabel, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────
        //  SCORE TABLE
        // ─────────────────────────────────────────────

        private void DrawScoreTable()
        {
            if (_entities.Count == 0) return;

            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("All Scores", _headerStyle);
            EditorGUILayout.Space(2);

            // Header
            EditorGUILayout.BeginHorizontal();
            var h = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
            GUILayout.Label("", h, GUILayout.Width(10));
            GUILayout.Label("Entity", h, GUILayout.Width(65));
            GUILayout.Label("Dist", h, GUILayout.Width(38));
            GUILayout.Label("LoS", h, GUILayout.Width(38));
            GUILayout.Label("DPS", h, GUILayout.Width(38));
            GUILayout.Label("Crd", h, GUILayout.Width(38));
            GUILayout.Label("Score", h, GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();

            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.3f, 0.3f, 0.3f));

            foreach (var entity in _entities.OrderByDescending(e => e.lastScore))
            {
                float dist = Vector2.Distance(_enemyPosition, entity.position);
                bool inRange = dist <= _detectionRadius;

                var rowStyle = new GUIStyle(EditorStyles.miniLabel);
                if (entity.isSelected)
                {
                    rowStyle.fontStyle = FontStyle.Bold;
                    rowStyle.normal.textColor = new Color(1f, 0.25f, 0.15f);
                }
                else if (!inRange)
                {
                    rowStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
                }

                EditorGUILayout.BeginHorizontal();

                // Dot
                Color dotCol = entity.type == EntityType.Player
                    ? new Color(0.3f, 0.55f, 1f) : new Color(1f, 0.8f, 0f);
                if (!inRange) dotCol *= 0.4f;
                var dotRect = GUILayoutUtility.GetRect(10, 14, GUILayout.Width(10));
                EditorGUI.DrawRect(new Rect(dotRect.x + 1, dotRect.y + 3, 7, 7), dotCol);

                GUILayout.Label(entity.name, rowStyle, GUILayout.Width(65));

                if (inRange)
                {
                    GUILayout.Label($"{entity.lastDistFactor:F2}", rowStyle, GUILayout.Width(38));
                    GUILayout.Label($"{entity.lastLosFactor:F2}", rowStyle, GUILayout.Width(38));
                    GUILayout.Label($"{entity.lastDpsFactor:F2}", rowStyle, GUILayout.Width(38));
                    GUILayout.Label($"{entity.lastCrowdFactor:F2}", rowStyle, GUILayout.Width(38));
                    GUILayout.Label($"{entity.lastScore:F3}", rowStyle, GUILayout.Width(45));
                }
                else
                {
                    for (int j = 0; j < 5; j++)
                        GUILayout.Label("—", rowStyle, GUILayout.Width(j < 4 ? 38 : 45));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        //  INPUT HANDLING
        // ─────────────────────────────────────────────

        private void HandleInput(Rect mapRect)
        {
            Event e = Event.current;
            if (!mapRect.Contains(e.mousePosition) && !_draggingEnemy && _draggedEntity == null)
                return;

            Vector2 mouseWorld = ScreenToWorld(e.mousePosition, mapRect);

            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0:
                {
                    if (Vector2.Distance(mouseWorld, _enemyPosition) < 1.5f)
                    {
                        _draggingEnemy = true;
                        _draggedEntity = null;
                        e.Use();
                        break;
                    }

                    for (int i = _entities.Count - 1; i >= 0; i--)
                    {
                        if (Vector2.Distance(mouseWorld, _entities[i].position) < 1.5f)
                        {
                            _draggedEntity = _entities[i];
                            _selectedIndex = i;
                            _draggingEnemy = false;
                            e.Use();
                            break;
                        }
                    }

                    if (_draggedEntity == null && !_draggingEnemy)
                    {
                        _selectedIndex = -1;
                        e.Use();
                    }

                    break;
                }

                case EventType.MouseDrag when e.button == 0:
                {
                    if (_draggingEnemy)
                    {
                        _enemyPosition = Clamp(mouseWorld);
                        Recalc();
                        e.Use();
                    }
                    else if (_draggedEntity != null)
                    {
                        _draggedEntity.position = Clamp(mouseWorld);
                        Recalc();
                        e.Use();
                    }

                    break;
                }

                case EventType.MouseUp when e.button == 0:
                {
                    if (_draggingEnemy || _draggedEntity != null)
                    {
                        _draggingEnemy = false;
                        _draggedEntity = null;
                        e.Use();
                    }

                    break;
                }

                case EventType.MouseDown when e.button == 1:
                {
                    for (int i = _entities.Count - 1; i >= 0; i--)
                    {
                        if (Vector2.Distance(mouseWorld, _entities[i].position) < 1.5f)
                        {
                            _entities.RemoveAt(i);
                            if (_selectedIndex == i) _selectedIndex = -1;
                            else if (_selectedIndex > i) _selectedIndex--;
                            Recalc();
                            e.Use();
                            break;
                        }
                    }

                    break;
                }
            }

            if (_draggingEnemy || _draggedEntity != null)
                Repaint();
        }

        // ─────────────────────────────────────────────
        //  CALCULATION
        // ─────────────────────────────────────────────

        private void Recalc()
        {
            if (_config == null) return;

            float totalWeight = _config.distanceWeight + _config.lineOfSightWeight + _config.dpsWeight + _config.crowdWeight;
            if (totalWeight <= 0f) totalWeight = 1f;

            float bestScore = 0f;
            TestEntity bestEntity = null;

            foreach (var entity in _entities)
            {
                float dist = Vector2.Distance(_enemyPosition, entity.position);
                if (dist > _detectionRadius)
                {
                    entity.lastScore = 0f;
                    entity.lastDistFactor = 0f;
                    entity.lastLosFactor = 0f;
                    entity.lastDpsFactor = 0f;
                    entity.lastCrowdFactor = 0f;
                    entity.isSelected = false;
                    continue;
                }

                entity.lastDistFactor = 1f - Mathf.Clamp01(dist / _detectionRadius);
                entity.lastLosFactor = entity.hasLineOfSight ? 1f : 0.2f;
                entity.lastDpsFactor = Mathf.Clamp01(entity.recentDPS / Mathf.Max(_config.maxDPSReference, 0.01f));
                entity.lastCrowdFactor = 1f - Mathf.Clamp01((float)entity.othersTargeting / Mathf.Max(_config.maxCrowdCount, 1));

                float score = 0f;
                score += entity.lastDistFactor * _config.distanceWeight;
                score += entity.lastLosFactor * _config.lineOfSightWeight;
                score += entity.lastDpsFactor * _config.dpsWeight;
                score += entity.lastCrowdFactor * _config.crowdWeight;
                score /= totalWeight;

                entity.lastScore = score;
                entity.isSelected = false;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestEntity = entity;
                }
            }

            if (bestEntity != null && bestScore >= _config.aggroThreshold)
                bestEntity.isSelected = true;

            Repaint();
        }

        // ─────────────────────────────────────────────
        //  DRAWING HELPERS
        // ─────────────────────────────────────────────

        private Vector2 WorldToScreen(Vector2 worldPos, Rect mapRect)
        {
            float nx = (worldPos.x / WORLD_SIZE + 0.5f) * mapRect.width + mapRect.x;
            float ny = (0.5f - worldPos.y / WORLD_SIZE) * mapRect.height + mapRect.y;
            return new Vector2(nx, ny);
        }

        private Vector2 ScreenToWorld(Vector2 screenPos, Rect mapRect)
        {
            float wx = ((screenPos.x - mapRect.x) / mapRect.width - 0.5f) * WORLD_SIZE;
            float wy = (0.5f - (screenPos.y - mapRect.y) / mapRect.height) * WORLD_SIZE;
            return new Vector2(wx, wy);
        }

        private Vector2 Clamp(Vector2 pos)
        {
            float half = WORLD_SIZE * 0.48f;
            return new Vector2(Mathf.Clamp(pos.x, -half, half), Mathf.Clamp(pos.y, -half, half));
        }

        private void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0), Vector3.forward, radius);
            Handles.EndGUI();
        }

        private void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawWireDisc(new Vector3(center.x, center.y, 0), Vector3.forward, radius, thickness);
            Handles.EndGUI();
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0));
            Handles.EndGUI();
        }
    }
}
#endif
