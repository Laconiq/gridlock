#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIWE.Modules;
using AIWE.Modules.Triggers;
using AIWE.Modules.Zones;
using AIWE.Modules.Effects;
using AIWE.NodeEditor.Data;
using AIWE.NodeEditor.UI;

namespace AIWE.Editor
{
    public static class FullSetup
    {
        [MenuItem("AIWE/Full Setup - Prefabs, Assets, Scene Wiring")]
        public static void RunFullSetup()
        {
            CreateModuleIcons();
            CreateModuleDefinitions();
            CreateModuleRegistry();
            CreateUIPrefabs();
            WireSceneReferences();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/GameScene.unity");
            Debug.Log("[FullSetup] Complete!");
        }

        // ========== MODULE ICONS (procedural textures) ==========

        static void CreateModuleIcons()
        {
            var dir = "Assets/Textures/ModuleIcons";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Textures"))
                    AssetDatabase.CreateFolder("Assets", "Textures");
                AssetDatabase.CreateFolder("Assets/Textures", "ModuleIcons");
            }

            // Triggers - orange
            CreateIcon(dir, "icon_on_timer", new Color(1f, 0.6f, 0.2f), IconShape.Clock);
            CreateIcon(dir, "icon_on_enemy_enter_range", new Color(1f, 0.5f, 0.15f), IconShape.Radar);

            // Zones - blue
            CreateIcon(dir, "icon_nearest_enemy", new Color(0.3f, 0.5f, 1f), IconShape.Crosshair);
            CreateIcon(dir, "icon_all_enemies_in_range", new Color(0.2f, 0.4f, 0.9f), IconShape.Circle);

            // Effects - green
            CreateIcon(dir, "icon_projectile", new Color(0.3f, 0.85f, 0.4f), IconShape.Arrow);
            CreateIcon(dir, "icon_hitscan", new Color(0.25f, 0.8f, 0.35f), IconShape.Lightning);
            CreateIcon(dir, "icon_slow", new Color(0.4f, 0.7f, 0.9f), IconShape.Snowflake);
            CreateIcon(dir, "icon_dot", new Color(0.9f, 0.3f, 0.3f), IconShape.Flame);

            AssetDatabase.Refresh();
            Debug.Log("[FullSetup] Module icons created");
        }

        enum IconShape { Clock, Radar, Crosshair, Circle, Arrow, Lightning, Snowflake, Flame }

        static void CreateIcon(string dir, string name, Color baseColor, IconShape shape)
        {
            var path = $"{dir}/{name}.png";
            if (File.Exists(path)) return;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            // Fill transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            var center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.4f;

            // Draw background circle
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / 2f);
                    pixels[y * size + x] = new Color(baseColor.r * 0.3f, baseColor.g * 0.3f, baseColor.b * 0.3f, alpha * 0.8f);
                }
            }

            // Draw shape
            switch (shape)
            {
                case IconShape.Clock:
                    DrawLine(pixels, size, center, center + new Vector2(0, radius * 0.6f), baseColor, 3);
                    DrawLine(pixels, size, center, center + new Vector2(radius * 0.4f, 0), baseColor, 3);
                    DrawCircleOutline(pixels, size, center, radius * 0.7f, baseColor, 2);
                    break;
                case IconShape.Radar:
                    DrawCircleOutline(pixels, size, center, radius * 0.3f, baseColor, 2);
                    DrawCircleOutline(pixels, size, center, radius * 0.55f, baseColor, 2);
                    DrawCircleOutline(pixels, size, center, radius * 0.8f, baseColor, 2);
                    DrawFilledCircle(pixels, size, center, 3, baseColor);
                    break;
                case IconShape.Crosshair:
                    DrawLine(pixels, size, center + Vector2.up * radius * 0.7f, center - Vector2.up * radius * 0.7f, baseColor, 2);
                    DrawLine(pixels, size, center + Vector2.right * radius * 0.7f, center - Vector2.right * radius * 0.7f, baseColor, 2);
                    DrawCircleOutline(pixels, size, center, radius * 0.4f, baseColor, 2);
                    break;
                case IconShape.Circle:
                    DrawCircleOutline(pixels, size, center, radius * 0.6f, baseColor, 3);
                    DrawFilledCircle(pixels, size, center, radius * 0.2f, baseColor);
                    break;
                case IconShape.Arrow:
                    DrawLine(pixels, size, center - Vector2.right * radius * 0.6f, center + Vector2.right * radius * 0.6f, baseColor, 3);
                    DrawLine(pixels, size, center + Vector2.right * radius * 0.6f, center + new Vector2(radius * 0.2f, radius * 0.3f), baseColor, 3);
                    DrawLine(pixels, size, center + Vector2.right * radius * 0.6f, center + new Vector2(radius * 0.2f, -radius * 0.3f), baseColor, 3);
                    break;
                case IconShape.Lightning:
                    var p1 = center + new Vector2(-radius * 0.15f, radius * 0.6f);
                    var p2 = center + new Vector2(radius * 0.1f, radius * 0.1f);
                    var p3 = center + new Vector2(-radius * 0.1f, -radius * 0.05f);
                    var p4 = center + new Vector2(radius * 0.15f, -radius * 0.6f);
                    DrawLine(pixels, size, p1, p2, baseColor, 3);
                    DrawLine(pixels, size, p2, p3, baseColor, 3);
                    DrawLine(pixels, size, p3, p4, baseColor, 3);
                    break;
                case IconShape.Snowflake:
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * 60f * Mathf.Deg2Rad;
                        var dir2 = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        DrawLine(pixels, size, center, center + dir2 * radius * 0.6f, baseColor, 2);
                    }
                    break;
                case IconShape.Flame:
                    DrawFilledCircle(pixels, size, center + Vector2.down * radius * 0.15f, radius * 0.35f, baseColor);
                    DrawFilledCircle(pixels, size, center + Vector2.up * radius * 0.1f, radius * 0.25f, baseColor);
                    DrawFilledCircle(pixels, size, center + Vector2.up * radius * 0.3f, radius * 0.15f, new Color(1f, 0.8f, 0.2f));
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void DrawLine(Color[] pixels, int size, Vector2 a, Vector2 b, Color col, int thickness)
        {
            int steps = (int)(Vector2.Distance(a, b) * 2);
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, (float)i / steps);
                for (int dx = -thickness; dx <= thickness; dx++)
                for (int dy = -thickness; dy <= thickness; dy++)
                {
                    int px = (int)p.x + dx, py = (int)p.y + dy;
                    if (px >= 0 && px < size && py >= 0 && py < size)
                        pixels[py * size + px] = col;
                }
            }
        }

        static void DrawCircleOutline(Color[] pixels, int size, Vector2 center, float radius, Color col, int thickness)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (Mathf.Abs(dist - radius) < thickness)
                    pixels[y * size + x] = col;
            }
        }

        static void DrawFilledCircle(Color[] pixels, int size, Vector2 center, float radius, Color col)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), center) < radius)
                    pixels[y * size + x] = col;
            }
        }

        // ========== MODULE DEFINITIONS ==========

        static void CreateModuleDefinitions()
        {
            // Triggers
            CreateTriggerDef("Assets/Data/Modules/Triggers/OnTimer.asset", "on_timer", "On Timer",
                "icon_on_timer", 2f, new Color(1f, 0.6f, 0.2f));
            CreateTriggerDef("Assets/Data/Modules/Triggers/OnEnemyEnterRange.asset", "on_enemy_enter_range", "On Enemy Enter",
                "icon_on_enemy_enter_range", 0.5f, new Color(1f, 0.5f, 0.15f));

            // Zones
            CreateZoneDef("Assets/Data/Modules/Zones/NearestEnemy.asset", "nearest_enemy", "Nearest Enemy",
                "icon_nearest_enemy", 15f, false, new Color(0.3f, 0.5f, 1f));
            CreateZoneDef("Assets/Data/Modules/Zones/AllEnemiesInRange.asset", "all_enemies_in_range", "All In Range",
                "icon_all_enemies_in_range", 12f, true, new Color(0.2f, 0.4f, 0.9f));

            // Effects
            CreateEffectDef("Assets/Data/Modules/Effects/Projectile.asset", "projectile", "Projectile",
                "icon_projectile", 15f, 0f, new Color(0.3f, 0.85f, 0.4f));
            CreateEffectDef("Assets/Data/Modules/Effects/Hitscan.asset", "hitscan", "Hitscan",
                "icon_hitscan", 20f, 0f, new Color(0.25f, 0.8f, 0.35f));
            CreateEffectDef("Assets/Data/Modules/Effects/Slow.asset", "slow", "Slow",
                "icon_slow", 0f, 3f, new Color(0.4f, 0.7f, 0.9f));
            CreateEffectDef("Assets/Data/Modules/Effects/DOT.asset", "dot", "Damage Over Time",
                "icon_dot", 5f, 4f, new Color(0.9f, 0.3f, 0.3f));

            Debug.Log("[FullSetup] Module definitions created");
        }

        static Sprite LoadIcon(string iconName)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath($"Assets/Textures/ModuleIcons/{iconName}.png");
            foreach (var s in sprites)
                if (s is Sprite sprite) return sprite;

            // Set texture import settings to Sprite
            var importer = AssetImporter.GetAtPath($"Assets/Textures/ModuleIcons/{iconName}.png") as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/ModuleIcons/{iconName}.png");
            }
            return null;
        }

        static void CreateTriggerDef(string path, string id, string displayName, string iconName, float cooldown, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<TriggerDefinition>(path) != null) return;
            var asset = ScriptableObject.CreateInstance<TriggerDefinition>();
            asset.moduleId = id;
            asset.displayName = displayName;
            asset.icon = LoadIcon(iconName);
            asset.category = ModuleCategory.Trigger;
            asset.nodeColor = color;
            asset.defaultCooldown = cooldown;
            asset.editableParams = new List<ModuleDefinition.ParamDef>
            {
                new() { name = "cooldown", defaultValue = cooldown, min = 0.1f, max = 10f }
            };
            AssetDatabase.CreateAsset(asset, path);
        }

        static void CreateZoneDef(string path, string id, string displayName, string iconName, float range, bool aoe, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<ZoneDefinition>(path) != null) return;
            var asset = ScriptableObject.CreateInstance<ZoneDefinition>();
            asset.moduleId = id;
            asset.displayName = displayName;
            asset.icon = LoadIcon(iconName);
            asset.category = ModuleCategory.Zone;
            asset.nodeColor = color;
            asset.defaultRange = range;
            asset.isAoE = aoe;
            asset.editableParams = new List<ModuleDefinition.ParamDef>
            {
                new() { name = "range", defaultValue = range, min = 1f, max = 50f }
            };
            AssetDatabase.CreateAsset(asset, path);
        }

        static void CreateEffectDef(string path, string id, string displayName, string iconName, float damage, float duration, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<EffectDefinition>(path) != null) return;
            var asset = ScriptableObject.CreateInstance<EffectDefinition>();
            asset.moduleId = id;
            asset.displayName = displayName;
            asset.icon = LoadIcon(iconName);
            asset.category = ModuleCategory.Effect;
            asset.nodeColor = color;
            asset.defaultDamage = damage;
            asset.defaultDuration = duration;
            var paramList = new List<ModuleDefinition.ParamDef>();
            if (damage > 0) paramList.Add(new ModuleDefinition.ParamDef { name = "damage", defaultValue = damage, min = 1f, max = 100f });
            if (duration > 0) paramList.Add(new ModuleDefinition.ParamDef { name = "duration", defaultValue = duration, min = 0.5f, max = 10f });
            asset.editableParams = paramList;
            AssetDatabase.CreateAsset(asset, path);
        }

        // ========== MODULE REGISTRY ==========

        static void CreateModuleRegistry()
        {
            var path = "Assets/Data/ModuleRegistry.asset";
            var registry = AssetDatabase.LoadAssetAtPath<ModuleRegistry>(path);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<ModuleRegistry>();
                AssetDatabase.CreateAsset(registry, path);
            }

            // Populate with all module definitions
            var so = new SerializedObject(registry);
            var list = so.FindProperty("allModules");
            list.ClearArray();

            var guids = AssetDatabase.FindAssets("t:ModuleDefinition", new[] { "Assets/Data/Modules" });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ModuleDefinition>(assetPath);
                if (def != null)
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = def;
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            Debug.Log($"[FullSetup] Module registry created with {list.arraySize} modules");
        }

        // ========== UI PREFABS ==========

        static void CreateUIPrefabs()
        {
            CreatePortWidgetPrefab();
            CreateNodeWidgetPrefab();
            CreateConnectionRendererPrefab();
            CreateModulePaletteItemPrefab();
            Debug.Log("[FullSetup] UI prefabs created");
        }

        static void CreatePortWidgetPrefab()
        {
            var path = "Assets/Prefabs/UI/PortWidget.prefab";
            EnsureFolder("Assets/Prefabs/UI");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = new GameObject("PortWidget");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(16, 16);

            var img = go.AddComponent<Image>();
            img.color = Color.white;
            // Make it circular via sprite if available, otherwise just a square
            img.raycastTarget = true;

            var port = go.AddComponent<PortWidget>();
            // Wire portImage
            var portSO = new SerializedObject(port);
            portSO.FindProperty("portImage").objectReferenceValue = img;
            portSO.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateNodeWidgetPrefab()
        {
            var path = "Assets/Prefabs/UI/NodeWidget.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = new GameObject("NodeWidget");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 80);
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

            // Header
            var header = new GameObject("Header");
            header.transform.SetParent(go.transform, false);
            var headerRT = header.AddComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 0.65f);
            headerRT.anchorMax = Vector2.one;
            headerRT.offsetMin = Vector2.zero;
            headerRT.offsetMax = Vector2.zero;
            var headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(1f, 0.6f, 0.2f);

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(header.transform, false);
            var iconRT = icon.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0);
            iconRT.anchorMax = new Vector2(0, 1);
            iconRT.offsetMin = new Vector2(4, 2);
            iconRT.offsetMax = new Vector2(28, -2);
            iconRT.sizeDelta = new Vector2(24, 0);
            var iconImg = icon.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;

            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            var titleRT = title.AddComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(30, 0);
            titleRT.offsetMax = new Vector2(-4, 0);
            var titleTMP = title.AddComponent<TextMeshProUGUI>();
            titleTMP.fontSize = 11;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.color = Color.white;
            titleTMP.text = "Module";

            // Input ports container (left side)
            var inputPorts = new GameObject("InputPorts");
            inputPorts.transform.SetParent(go.transform, false);
            var ipRT = inputPorts.AddComponent<RectTransform>();
            ipRT.anchorMin = new Vector2(0, 0);
            ipRT.anchorMax = new Vector2(0, 0.65f);
            ipRT.pivot = new Vector2(0, 0.5f);
            ipRT.offsetMin = new Vector2(-8, 8);
            ipRT.offsetMax = new Vector2(8, -8);
            var ipLayout = inputPorts.AddComponent<VerticalLayoutGroup>();
            ipLayout.spacing = 4;
            ipLayout.childAlignment = TextAnchor.MiddleLeft;

            // Output ports container (right side)
            var outputPorts = new GameObject("OutputPorts");
            outputPorts.transform.SetParent(go.transform, false);
            var opRT = outputPorts.AddComponent<RectTransform>();
            opRT.anchorMin = new Vector2(1, 0);
            opRT.anchorMax = new Vector2(1, 0.65f);
            opRT.pivot = new Vector2(1, 0.5f);
            opRT.offsetMin = new Vector2(-8, 8);
            opRT.offsetMax = new Vector2(8, -8);
            var opLayout = outputPorts.AddComponent<VerticalLayoutGroup>();
            opLayout.spacing = 4;
            opLayout.childAlignment = TextAnchor.MiddleRight;

            // Add NodeWidget component and wire
            var nodeWidget = go.AddComponent<NodeWidget>();
            var portPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PortWidget.prefab");

            var nwSO = new SerializedObject(nodeWidget);
            nwSO.FindProperty("headerImage").objectReferenceValue = headerImg;
            nwSO.FindProperty("titleText").objectReferenceValue = titleTMP;
            nwSO.FindProperty("iconImage").objectReferenceValue = iconImg;
            nwSO.FindProperty("inputPortsContainer").objectReferenceValue = ipRT;
            nwSO.FindProperty("outputPortsContainer").objectReferenceValue = opRT;
            if (portPrefab != null)
                nwSO.FindProperty("portPrefab").objectReferenceValue = portPrefab.GetComponent<PortWidget>();
            nwSO.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateConnectionRendererPrefab()
        {
            var path = "Assets/Prefabs/UI/ConnectionRenderer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = new GameObject("ConnectionRenderer");
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var conn = go.AddComponent<ConnectionRenderer>();
            conn.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            conn.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateModulePaletteItemPrefab()
        {
            var path = "Assets/Prefabs/UI/ModulePaletteItem.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = new GameObject("ModulePaletteItem");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 32);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 32;
            layout.flexibleWidth = 1;

            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.28f, 0.9f);

            // Color bar (left edge)
            var colorBar = new GameObject("ColorBar");
            colorBar.transform.SetParent(go.transform, false);
            var cbRT = colorBar.AddComponent<RectTransform>();
            cbRT.anchorMin = Vector2.zero;
            cbRT.anchorMax = new Vector2(0, 1);
            cbRT.offsetMin = Vector2.zero;
            cbRT.offsetMax = new Vector2(4, 0);
            cbRT.sizeDelta = new Vector2(4, 0);
            var cbImg = colorBar.AddComponent<Image>();
            cbImg.color = Color.white;

            // Name text
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(go.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = Vector2.zero;
            nameRT.anchorMax = Vector2.one;
            nameRT.offsetMin = new Vector2(8, 0);
            nameRT.offsetMax = new Vector2(-4, 0);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.fontSize = 11;
            nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            nameTMP.color = Color.white;
            nameTMP.text = "Module";
            nameTMP.raycastTarget = false;

            var paletteItem = go.AddComponent<ModulePaletteItem>();
            var piSO = new SerializedObject(paletteItem);
            piSO.FindProperty("nameText").objectReferenceValue = nameTMP;
            piSO.FindProperty("colorBar").objectReferenceValue = cbImg;
            piSO.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ========== SCENE WIRING ==========

        static void WireSceneReferences()
        {
            // Load prefabs
            var nodeWidgetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/NodeWidget.prefab");
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ConnectionRenderer.prefab");
            var paletteItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ModulePaletteItem.prefab");
            var moduleRegistry = AssetDatabase.LoadAssetAtPath<ModuleRegistry>("Assets/Data/ModuleRegistry.asset");
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/BasicProjectile.prefab");

            // Wire NodeEditorCanvas
            var canvasObj = Object.FindFirstObjectByType<NodeEditorCanvas>();
            if (canvasObj != null)
            {
                var so = new SerializedObject(canvasObj);
                if (nodeWidgetPrefab != null)
                    so.FindProperty("nodeWidgetPrefab").objectReferenceValue = nodeWidgetPrefab.GetComponent<NodeWidget>();
                if (connectionPrefab != null)
                    so.FindProperty("connectionPrefab").objectReferenceValue = connectionPrefab.GetComponent<ConnectionRenderer>();
                so.ApplyModifiedProperties();
                Debug.Log("[FullSetup] Wired NodeEditorCanvas");
            }

            // Wire ModulePalette
            var palette = Object.FindFirstObjectByType<ModulePalette>();
            if (palette != null)
            {
                var so = new SerializedObject(palette);
                if (paletteItemPrefab != null)
                    so.FindProperty("paletteItemPrefab").objectReferenceValue = paletteItemPrefab.GetComponent<ModulePaletteItem>();
                if (moduleRegistry != null)
                    so.FindProperty("moduleRegistry").objectReferenceValue = moduleRegistry;
                so.ApplyModifiedProperties();
                Debug.Log("[FullSetup] Wired ModulePalette");
            }

            // Wire TowerExecutor on SentinelleTower
            var executors = Object.FindObjectsByType<Towers.TowerExecutor>(FindObjectsSortMode.None);
            foreach (var exec in executors)
            {
                var so = new SerializedObject(exec);
                if (moduleRegistry != null)
                    so.FindProperty("moduleRegistry").objectReferenceValue = moduleRegistry;
                if (projectilePrefab != null)
                    so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
                so.ApplyModifiedProperties();
            }
            Debug.Log($"[FullSetup] Wired {executors.Length} TowerExecutors");

            // Wire EnemySpawner
            var spawner = Object.FindFirstObjectByType<Enemies.EnemySpawner>();
            if (spawner != null)
            {
                var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BasicEnemy.prefab");
                var so = new SerializedObject(spawner);
                if (enemyPrefab != null)
                    so.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[FullSetup] Scene wiring complete");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
