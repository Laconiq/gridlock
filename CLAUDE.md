# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AIWE is an **isometric grid-based Tower Defense** built in Unity 6 (6000.3.10f1) with URP. The player defends an objective against enemy waves by placing up to 5 towers anywhere on the grid and configuring them via a visual node editor. Isometric orthographic camera (30° pitch, 45° yaw). Enemies are tetrahedrons that follow predefined paths on the grid. Towers are cubes with default URP/Lit material. All meshes are baked into prefabs (no runtime mesh generation). Heavy Bloom post-processing for neon aesthetic.

## Stack & Tools

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6 (6000.3.10f1) |
| Rendering | URP 17.x |
| Input | New Input System 1.19.0 (Controls asset with callbacks) |
| Level design | Grid system (`GridDefinition` ScriptableObjects) |
| Visuals | URP/Lit materials + CyberGrid shader + Bloom post-process |
| Scripting backend | IL2CPP |
| Inspector | Odin Inspector (Sirenix) |
| Node Editor UI | Unity UI Toolkit (USS/UXML) |
| UI Font | Space Grotesk (variable weight) |

**No networking.** The project is single-player only.

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. The code should be self-documenting.
- **No custom Inspector scripts.** Do not create custom editors, property drawers, or Inspector scripts — use Odin attributes and ScriptableObjects instead. Exceptions: `#if UNITY_EDITOR`-guarded editor tools (e.g. `CreateTestGrid`, `BakePrefabMeshes`) are allowed when they serve the pipeline.
- **Module system uses `[SerializeReference]` + Odin.** Module definitions use `[SerializeReference, ListDrawerSettings, InlineProperty] List<T>` for concrete implementations. The SO holds templates, `CreateInstance()` clones them at runtime. No string-based factory dispatch.
- **All gameplay values are `[SerializeField]`** for live Inspector tuning. No magic numbers in code.
- **UI colors use design tokens.** USS uses `var(--token)` from `DesignTokens.uss`. C# uses `DesignConstants` static fields. Never hardcode hex colors.
- **Prefab meshes are baked.** Enemy/tower/projectile meshes are assigned directly in the prefab via editor tools (`BakePrefabMeshes`). No runtime mesh generation on prefabs. Use Unity's Default-Lit material.
- **Input uses Controls asset.** Never use `UnityEngine.Input`, `Keyboard.current`, or `Mouse.current` directly. Always use the `Controls` class generated from `Controls.inputactions` with proper callbacks (started/canceled).

## Architecture

### Game flow

```
SimpleGameBootstrap.Start()
  → Instantiate PlayerPrefab (invisible command entity)
  → GameManager.SetState(Preparing)
  → Player places towers (up to 5) on the grid via TowerPlacementSystem
  → Each tower gets a default loadout (OnTimer → NearestEnemy → Projectile)
  → Player can click towers to configure them via NodeEditorScreen
  → Player clicks "Start Wave" (WaveStartUI)
  → GameManager.SetState(Wave)
  → Enemies spawn, follow routes segment-by-segment
  → Towers fire automatically based on their node graph
  → Enemies die → loot flies to camera center (magnet auto-collect)
  → Wave cleared → GameManager.SetState(Preparing)
  → Objective HP depleted → GameManager.SetState(GameOver)
```

### Grid System

`GridDefinition` SO (`Assets/Scripts/Grid/GridDefinition.cs`):
- Defines grid dimensions (width, height, cellSize), cell types array, path definitions, objective HP
- Cell types: `Empty`, `Path`, `TowerSlot`, `Blocked`, `Spawn`, `Objective`
- Paths are ordered `List<Vector2Int>` waypoints on the grid
- Current test level: 24x14, cellSize=2 (48x28 world units)

`GridManager` (`Assets/Scripts/Grid/GridManager.cs`):
- ServiceLocator singleton, converts grid↔world coordinates
- Provides routes, spawn positions, objective position

`TowerPlacementSystem` (`Assets/Scripts/Grid/TowerPlacementSystem.cs`):
- **5 towers max**, placeable on any `Empty` or `TowerSlot` cell
- Left click on empty ground → place tower with default loadout (OnTimer→NearestEnemy→Projectile)
- Left click on placed tower → open node editor (via `IInteractable`)
- Preview cube follows mouse (green=valid, red=invalid)
- UI click-through protection via `EventSystem.RaycastAll`
- Only active during `GameState.Preparing`

### Camera (isometric)

`CameraSetup` (`Assets/Scripts/Camera/CameraSetup.cs`):
- Sets orthographic, black background, size 14
- Rotation: (30°, 45°, 0°) — classic isometric angle

`TopDownCamera` (`Assets/Scripts/Camera/TopDownCamera.cs`):
- **Middle mouse drag** to pan (screen-space axes via `transform.right` + cross product)
- **Scroll wheel** to zoom (smooth lerp to target orthographic size)
- No WASD, no edge panning
- Uses `Controls` asset with `CameraPan`, `CameraDelta`, `CameraZoom` actions
- Bounds clamping to grid area

### Player (command entity)

No player character on the field. The "player" is an invisible entity that manages:
- `PlayerController` — input state and game state listening
- `PlayerInteraction` — mouse raycast to detect/click towers
- `PlayerInventory` — module inventory (simple `List<ModuleSlot>`)
- `PlayerInputProvider` — New Input System integration

### Module system (core gameplay)

Towers are empty chassis configured via a visual node editor. Three module types chain together:

```
Trigger (WHEN) ──► Zone (WHERE/WHO) ──► Effect (WHAT)
                   Zone ──► Zone (serial chaining)
                   Effect ──► Effect (vertical chaining)
```

**Definition SOs** hold `[SerializeReference] List<T>` of `[Serializable]` runtime classes:
- `TriggerDefinition` → `List<TriggerInstance>` (e.g. OnTimerTrigger, OnEnemyEnterRangeTrigger)
- `ZoneDefinition` → `List<ZoneInstance>` (e.g. NearestEnemyZone, AllEnemiesInRangeZone)
- `EffectDefinition` → `List<EffectInstance>` (e.g. ProjectileEffect, HitscanEffect, SlowEffect, DotEffect)

**Default tower loadout:** When a tower is placed, `TowerPlacementSystem.CreateDefaultGraph()` builds a graph with `on_timer → nearest_enemy → projectile` and applies it via `TowerChassis.SetNodeGraph()`.

**Execution pipeline** (in `TowerExecutor`):
1. `RebuildFromGraph(NodeGraphData)` walks the graph from Trigger nodes
2. For each Trigger → follows connections to Zones → collects Effects
3. Builds `TriggerChain → ZoneChain → List<EffectInstance>` tree (max depth 8)
4. Each frame, triggers tick. On fire → zones select targets → effects execute on targets

**Tower prefab requirements:** `TowerChassis` needs `definition` (ChassisDefinition SO) and `firePoint` (child Transform). `TowerExecutor` needs `moduleRegistry` (ModuleRegistry SO). These are baked into the prefab via `FixTowerPrefab` editor tool.

### Tower interaction flow

```
TowerPlacementSystem (left click raycast) → IInteractable found (TowerInteractable)
  → Click → NodeEditorScreen.Open(chassis, inventory)
  → Save & Close → graph saved to TowerChassis → TowerExecutor.RebuildFromGraph()
  → PlayerInventory adjusted (module count delta)
```

### Node editor UI (UI Toolkit)

The node editor uses **Unity UI Toolkit** (not legacy Canvas/UGUI). All UI is built with USS/UXML + C# VisualElements.

**Design system:**
- `Assets/UI/NodeEditor/DesignTokens.uss` — Single source of truth for all colors, spacing, typography
- `Assets/UI/NodeEditor/NodeEditor.uss` — All component styles, references tokens only
- `Assets/Scripts/NodeEditor/UI/DesignConstants.cs` — C# mirror of tokens

### ServiceLocator

Static dictionary for global singletons. Registered: `GameManager`, `ObjectiveController`, `RouteManager`, `GridManager`.

```csharp
ServiceLocator.Register<T>(instance);  // in Awake
ServiceLocator.Get<T>();               // anywhere
ServiceLocator.Unregister<T>();        // in OnDestroy
```

### Enemy system

**Movement:** `EnemyController` follows routes segment-by-segment via `AssignRoute()`. Uses a `while` loop consuming remaining movement per frame — never cuts diagonals. Enemies do **not rotate** when changing direction (model stays fixed). Float height at Y=0.5.

**AI State machine** (`EnemyAI`): `FollowRoute → ChaseTarget → Attack → ReturnToRoute`
- FollowRoute: movement handled entirely by `EnemyController.FollowRouteStep()`
- ReturnToRoute: finds nearest waypoint, walks there, then resumes route via `AssignRoute`

**Threat evaluation** (in `ThreatCalculator`, called from `EnemyAI`):
- `ThreatSource` components on towers track recent DPS (exponential decay)
- Config via `ThreatCalculatorConfig` ScriptableObject

**Spawning:** `EnemySpawner` instantiates the prefab as-is (no runtime material/mesh changes). Position, color, mesh, scale are all defined in the prefab.

### Level design (Grid)

Levels are `GridDefinition` ScriptableObjects edited in Odin Inspector.

**Grid structure:**
- Flat `CellType[]` array indexed by `[y * width + x]`
- `PathDefinition` lists: ordered `Vector2Int` waypoints per route
- Cell types: Empty, Path, TowerSlot, Blocked, Spawn, Objective

**Runtime flow:**
1. `GridManager.Awake()` reads `GridDefinition`, converts grid coords to world positions
2. `GridVisual` spawns a scaled Quad with `CyberGrid.shader` as ground plane
3. `PathVisualizer` draws LineRenderer paths from `RouteManager` data
4. `TowerPlacementSystem` enables tower placement on Empty/TowerSlot cells during Preparing state
5. `EnemySpawner` spawns enemies at `Spawn` cells, `EnemyController` follows routes to `Objective`

### Key interfaces

| Interface | Contract | Implementors |
|-----------|----------|-------------|
| `IChassis` | Tower config container (get/set graph, fire point, max triggers, base range) | TowerChassis |
| `IInteractable` | Interaction prompt + permission + action | TowerInteractable |
| `ITargetable` | Position, IsAlive, Transform for targeting | EnemyController |
| `IDamageable` | TakeDamage(DamageInfo) | EnemyHealth, ObjectiveController |

### Loot system

Enemies drop `ModulePickup` on death. Pickups have a magnet behavior: after a short delay, they fly toward the camera's ground-projected center and auto-add to the player's `PlayerInventory`.

## Key Files

- `Controls.inputactions` / `Controls.cs` — Input bindings. **Do not edit Controls.cs** (auto-generated)
- `Assets/Scenes/GameScene.unity` — Main game scene (build index 0)
- `Assets/Data/Modules/` — Module definition SOs (Triggers/, Zones/, Effects/)
- `Assets/Data/ModuleRegistry.asset` — Registry referencing all module definitions
- `Assets/Data/Chassis/Sentinelle.asset` — Default tower ChassisDefinition
- `Assets/Data/DefaultLoadout.asset` — Starter module kit given to players on spawn
- `Assets/Scripts/Camera/CameraSetup.cs` — Isometric camera setup (30°, 45°)
- `Assets/Scripts/Camera/TopDownCamera.cs` — Camera pan/zoom with New Input System
- `Assets/Scripts/Grid/GridDefinition.cs` — Grid level data (SO)
- `Assets/Scripts/Grid/GridManager.cs` — Grid runtime manager (ServiceLocator singleton)
- `Assets/Scripts/Grid/TowerPlacementSystem.cs` — Tower placement (5 max, default loadout, UI protection)
- `Assets/Scripts/Grid/GridVisual.cs` — Ground plane with CyberGrid shader
- `Assets/Scripts/Core/SimpleGameBootstrap.cs` — Game initialization
- `Assets/Scripts/Core/GameStats.cs` — Kill/wave tracking singleton
- `Assets/Data/Levels/TestGrid.asset` — Test level GridDefinition (24x14, cellSize=2)
- `Assets/Meshes/Tetrahedron.asset` — Baked enemy mesh
- `Assets/UI/NodeEditor/DesignTokens.uss` — **Design system tokens**
- `Assets/Scripts/NodeEditor/UI/DesignConstants.cs` — **C# design constants**
- `Assets/UI/HUD/GameHUD.uxml` — HUD layout with tower selection bar
- `Assets/UI/HUD/WaveStart.uxml` — Wave start button overlay
- `Assets/Shaders/CyberGrid.shader` — Grid ground plane shader (world-space XZ lines)
- `Assets/Shaders/VectorGlow.shader` — Lit shader with emission + shadows for enemies
- `Assets/Shaders/VectorOutline.shader` — Lit outline shader with shadows for towers
- `Assets/Scripts/AI/ThreatCalculatorConfig` — Threat evaluation weights (ScriptableObject)

## Editor Tools

These are `#if UNITY_EDITOR` menu items under `AIWE/`:
- **Create Test Grid Level** — Generates a 24x14 GridDefinition SO with S-path and tower slots
- **Bake Meshes into Prefabs** — Assigns Tetrahedron/Cube/Sphere meshes + Default-Lit material to enemy/tower/projectile prefabs
- **Fix Tower Prefab References** — Links ChassisDefinition, FirePoint, ModuleRegistry on tower prefab
- **Assign Default Material to Prefabs** — Sets Unity's default URP/Lit material on all gameplay prefabs

## Git Workflow (Gitflow)

- **`main`** — Production-ready. Protected, never commit directly.
- **`dev`** — Integration branch. All feature branches merge here.
- **`feature/<name>`** — Branch from `dev`, merge back via PR (squash merge).
- **`release/<version>`** — Branch from `dev`, merge into `main` and `dev`.
- **`hotfix/<name>`** — Branch from `main`, merge into `main` and `dev`.

PRs target `dev` by default. Delete feature branches after merge.
