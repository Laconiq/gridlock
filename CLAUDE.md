# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Gridlock is an **isometric grid-based Tower Defense** built in Unity 6 (6000.3.10f1) with URP. The player defends an objective against enemy waves by placing up to 5 towers anywhere on the grid and configuring them via a visual node editor. Isometric orthographic camera (30° pitch, 45° yaw). Enemies are tetrahedrons that follow predefined paths on the grid (classic TD — no AI targeting/chasing). Towers are cubes with default URP/Lit material. Neon aesthetic with Geometry Wars-style grid deformation, bloom, screen shake, and chromatic aberration.

## Stack & Tools

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6 (6000.3.10f1) |
| Rendering | URP 17.x |
| Input | New Input System 1.19.0 (Controls asset with callbacks) |
| Level design | Grid system (`GridDefinition` ScriptableObjects) |
| Visuals | URP/Lit materials + CyberGrid shader + Bloom post-process |
| Juice / Feel | More Mountains Feel (MMFeedbacks) — installed but feedbacks are triggered via code, not MMF_Player prefabs |
| Scripting backend | IL2CPP |
| Inspector | Odin Inspector (Sirenix) |
| Node Editor UI | Unity UI Toolkit (USS/UXML) |
| UI Font | Space Grotesk (variable weight) |

**No networking.** The project is single-player only.

## UI Design Workflow

When asked to create or redesign a UI screen (not minor tweaks):
1. Use **Stitch MCP** tools to generate the design from a prompt (produces HTML/screenshot)
2. Convert the HTML output into **UXML/USS** compatible with the existing Unity design system (`DesignTokens.uss`, `DesignConstants.cs`)

For minor UI modifications (tweaking spacing, colors, adding a label), skip Stitch and edit UXML/USS directly.

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. The code should be self-documenting. Section headers in enums/constants are OK. Remove any comment that restates what the code does (e.g. `// clean the player prefab` before a line that obviously cleans the player prefab). When modifying a file, proactively remove superfluous comments encountered in the diff area.
- **No custom Inspector scripts.** Do not create custom editors, property drawers, or Inspector scripts — use Odin attributes and ScriptableObjects instead. Exceptions: `#if UNITY_EDITOR`-guarded editor tools (e.g. `CreateTestGrid`, `BakePrefabMeshes`) are allowed when they serve the pipeline.
- **Module system uses `[SerializeReference]` + Odin.** Module definitions use `[SerializeReference, ListDrawerSettings, InlineProperty] List<T>` for concrete implementations. The SO holds templates, `CreateInstance()` clones them at runtime. No string-based factory dispatch.
- **All gameplay values are `[SerializeField]`** for live Inspector tuning. No magic numbers in code.
- **UI colors use design tokens.** USS uses `var(--token)` from `DesignTokens.uss`. C# uses `DesignConstants` static fields. Never hardcode hex colors.
- **Prefab meshes are baked.** Enemy/tower/projectile meshes are assigned directly in the prefab via editor tools (`BakePrefabMeshes`). No runtime mesh generation on prefabs.
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
  → Enemies spawn, follow routes segment-by-segment (no AI targeting)
  → Towers fire automatically based on their node graph
  → Enemies die → voxel death explosion → loot flies to camera center (magnet auto-collect)
  → Wave cleared → GameManager.SetState(Preparing)
  → Objective HP depleted → GameManager.SetState(GameOver)
```

### Grid System

`GridDefinition` SO (`Assets/Scripts/Grid/GridDefinition.cs`):
- Defines grid dimensions (width, height, cellSize), cell types array, path definitions, objective HP
- Implements `ISerializationCallbackReceiver` — saves original cells on deserialize, `CloneCells()` always returns the clean copy (prevents SO corruption from runtime modifications)
- Cell types: `Empty`, `Path`, `TowerSlot`, `Blocked`, `Spawn`, `Objective`
- Paths are ordered `List<Vector2Int>` waypoints on the grid
- Current test level: 24x14, cellSize=2 (48x28 world units)

`GridManager` (`Assets/Scripts/Grid/GridManager.cs`):
- ServiceLocator singleton, converts grid↔world coordinates
- **Runtime cell state**: `_runtimeCells` cloned from SO at startup, never modifies the SO
- `GetRuntimeCell(x,y)` / `SetRuntimeCell(x,y,type)` for runtime queries/mutations
- `TryWorldToGrid(worldPos, out gridPos)` — returns false if out of bounds (no clamping)
- `OnCellChanged` event fires when runtime cells change → drives `GridVisual` cell map updates
- Provides routes, spawn positions, objective position

`TowerPlacementSystem` (`Assets/Scripts/Grid/TowerPlacementSystem.cs`):
- **5 towers max**, placeable on any `Empty` or `TowerSlot` cell
- Runs in `LateUpdate()` (synced with camera)
- Uses `TryWorldToGrid()` — rejects out-of-bounds clicks instead of clamping
- `LayerMask` for tower click raycasting
- Tower pop-in scale animation on placement
- Triggers grid warp + GameJuice on placement
- Preview cube follows mouse (green=valid, red=invalid), hides when cursor exits grid
- UI click-through protection via `EventSystem.RaycastAll`
- Only active during `GameState.Preparing`

### Grid Visual System

`GridVisual` (`Assets/Scripts/Grid/GridVisual.cs`):
- Creates a **subdivided mesh** (~1.5 vertices per world unit) for grid warp deformation
- Generates a cell map `Texture2D` (1 pixel per cell, point-filtered) for cell type visualization
- Listens to `GridManager.OnCellChanged` to update cell map in real-time
- Cell colors: Path=magenta lines, Blocked=red lines, Spawn/Objective=bright tints
- Passes mesh to `GridWarpManager` for physics simulation

`CyberGrid.shader` (`Assets/Shaders/CyberGrid.shader`):
- Transparent shader (alpha blend) — only grid lines visible, black between them
- Procedural anti-aliased grid lines from deformed world-space XZ coordinates
- Reads vertex colors from mesh → grid lines change color near warp events
- Samples cell map texture for path/blocked cell visualization
- Grid line brightness increases near active warp events (glow effect)

`GridWarpManager` (`Assets/Scripts/Grid/GridWarpManager.cs`):
- **Mass-spring-damper physics** on every vertex of the grid mesh (CPU)
- Each vertex: anchor spring (back to rest) + 4 neighbor springs (wave propagation)
- `DropStone(pos, force, radius, color)` — stone-in-water ripple effect
- `Shockwave(pos, force, radius, color)` — XZ outward push + Y dip
- `GetWarpOffset(worldX, worldZ)` — bilinear interpolation for any world position
- **Tint diffusion**: color spreads between neighbors like ink in water, brightness follows wave displacement
- Key tuning: `anchorStiffness` (low=flexible), `neighborStiffness` (high=fast propagation), `damping` (low=long oscillation)

`WarpFollower` (`Assets/Scripts/Visual/WarpFollower.cs`):
- Lightweight component added to any object that should follow the grid surface
- Samples `GridWarpManager.GetWarpOffset()` each `LateUpdate` and adjusts Y position
- Auto-added to: enemies (in `EnemyController.Awake`), towers (at placement), projectiles (in `Projectile.Initialize`)

`PathVisualizer` (`Assets/Scripts/Visual/PathVisualizer.cs`):
- Draws LineRenderer + dot spheres for enemy routes
- Updates positions every `LateUpdate` to follow grid warp deformation

### Juice System

`GameJuice` (`Assets/Scripts/Visual/GameJuice.cs`):
- Singleton managing all game feel effects
- **Screen shake**: `[DefaultExecutionOrder(200)]` runs after camera, applies position offset
- **Freeze frame**: brief `Time.timeScale = 0` on kills
- **Chromatic aberration pulse**: decays over time via URP Volume API
- **Bloom pulse**: boost + decay on kills
- **Vignette**: permanent subtle darkening at screen edges
- **Grid warp triggers**: calls `GridWarpManager.DropStone/Shockwave` with event-specific colors

| Event | Shake | Freeze | Grid Warp | Color |
|-------|-------|--------|-----------|-------|
| Enemy hit | subtle | — | DropStone small | orange |
| Enemy killed | medium | 40ms | Shockwave large | red |
| Tower fired | — | — | DropStone tiny | cyan |
| Tower placed | — | — | Shockwave medium | green |

`ImpactFlash` (`Assets/Scripts/Visual/ImpactFlash.cs`):
- Static `Spawn(position, color)` — creates expanding glowing sphere + point light at projectile impact
- Fades and destroys over 150ms

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

**Tower prefab requirements:** `TowerChassis` has `maxTriggers` and `baseRange` as `[SerializeField]` fields, plus `firePoint` (child Transform). `TowerExecutor` needs `moduleRegistry` (ModuleRegistry SO). These are baked into the prefab via `FixTowerPrefab` editor tool.

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

**Classic TD enemies — no AI targeting.** Enemies follow routes to the objective. No chase, no attack, no threat evaluation.

**Single prefab, data-driven via `EnemyDefinition` SO:**
```
Enemy.prefab (root) — logic + death VFX
├── EnemyController, EnemyHealth, StatusEffectManager, EnemyHitFeedback, EnemyAI, VoxelDeathEffect
└── Model (child) — visual only
    ├── MeshFilter (default mesh, overridden at spawn from SO)
    └── MeshRenderer (default material, overridden at spawn from SO)
```
Visuals (mesh, material, color, scale) come from `EnemyDefinition` SO, applied by `EnemySpawner` at instantiation.

**Movement:** `EnemyController` follows routes segment-by-segment via `AssignRoute()`. Uses a `while` loop consuming remaining movement per frame — never cuts diagonals. Float height at Y=0.5. `WarpFollower` auto-added in Awake for grid surface tracking.

**AI** (`EnemyAI`): Simplified to pure route following. `Setup()` assigns route and starts movement. No states other than `FollowRoute`.

**Death effects:** `VoxelDeathEffect` (on Enemy.prefab) voxelizes the mesh into physics cubes that explode outward, bounce, and fade. Uses `GetComponentInChildren<MeshFilter/MeshRenderer>()` to find the Model child.

**Hit feedback:** `EnemyHitFeedback` flashes emission white on hit, spawns floating damage text. Uses `GetComponentInChildren<MeshRenderer>()`.

**Spawning:** `EnemySpawner` instantiates `Enemy.prefab`, then applies visuals (mesh, material, color, scale) from the `EnemyDefinition` SO. Stats (HP, speed, objective damage) are also set from the SO.

### Combat

**DamageInfo** struct: `Amount` (float) + `Type` (Direct, Projectile, Hitscan, DamageOverTime). No SourceId — threat system was removed.

**Projectile:** Homing to target, SphereCast collision, spawns `ImpactFlash` on hit, triggers `GameJuice.OnEnemyHit`. `WarpFollower` auto-added for grid surface tracking.

### Level design (Grid)

Levels are `GridDefinition` ScriptableObjects edited in Odin Inspector.

**Grid structure:**
- Flat `CellType[]` array indexed by `[y * width + x]`
- `PathDefinition` lists: ordered `Vector2Int` waypoints per route
- Cell types: Empty, Path, TowerSlot, Blocked, Spawn, Objective

**Runtime flow:**
1. `GridManager.Awake()` clones cells from `GridDefinition` SO (protects SO from runtime corruption)
2. `GridVisual` creates subdivided mesh + cell map texture, initializes `GridWarpManager`
3. `PathVisualizer` draws LineRenderer paths from `RouteManager` data (updates with grid warp)
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
- `Assets/Scripts/Towers/TowerChassis.cs` — Tower config (maxTriggers, baseRange, node graph)
- `Assets/Data/DefaultLoadout.asset` — Starter module kit given to players on spawn
- `Assets/Scripts/Camera/CameraSetup.cs` — Isometric camera setup (30°, 45°)
- `Assets/Scripts/Camera/TopDownCamera.cs` — Camera pan/zoom with New Input System
- `Assets/Scripts/Grid/GridDefinition.cs` — Grid level data (SO with ISerializationCallbackReceiver)
- `Assets/Scripts/Grid/GridManager.cs` — Grid runtime manager (runtime cells, events, ServiceLocator singleton)
- `Assets/Scripts/Grid/GridVisual.cs` — Subdivided grid mesh + cell map texture
- `Assets/Scripts/Grid/GridWarpManager.cs` — Mass-spring grid deformation (Geometry Wars style)
- `Assets/Scripts/Grid/TowerPlacementSystem.cs` — Tower placement (5 max, LateUpdate, pop-in animation)
- `Assets/Scripts/Visual/GameJuice.cs` — Screen shake, freeze frame, bloom/chromatic pulses, grid warp triggers
- `Assets/Scripts/Visual/ImpactFlash.cs` — Projectile impact glow effect
- `Assets/Scripts/Visual/WarpFollower.cs` — Makes objects follow grid warp surface
- `Assets/Scripts/Visual/VoxelDeathEffect.cs` — Enemy death voxel explosion
- `Assets/Scripts/Visual/PathVisualizer.cs` — Route LineRenderer + dots (warp-aware)
- `Assets/Scripts/Core/SimpleGameBootstrap.cs` — Game initialization
- `Assets/Scripts/Core/GameStats.cs` — Kill/wave tracking singleton
- `Assets/Data/Levels/TestGrid.asset` — Test level GridDefinition (24x14, cellSize=2)
- `Assets/Meshes/Tetrahedron.asset` — Baked enemy mesh
- `Assets/UI/NodeEditor/DesignTokens.uss` — **Design system tokens**
- `Assets/Scripts/NodeEditor/UI/DesignConstants.cs` — **C# design constants**
- `Assets/UI/HUD/GameHUD.uxml` — HUD layout with tower selection bar
- `Assets/UI/HUD/WaveStart.uxml` — Wave start button overlay
- `Assets/Shaders/CyberGrid.shader` — Grid shader (transparent lines, vertex color warp glow, cell map)
- `Assets/Shaders/VectorGlow.shader` — Lit shader with emission + shadows for enemies
- `Assets/Shaders/VectorOutline.shader` — Lit outline shader with shadows for towers
- `Assets/Prefabs/Enemies/Enemy.prefab` — Base enemy prefab (logic on root, Model child for visuals)
- `Assets/Data/Enemies/TestEnemy.asset` — Test EnemyDefinition SO (mesh, material, color, scale, stats)

## Editor Tools

These are `#if UNITY_EDITOR` menu items under `Gridlock/`:
- **Create Test Grid Level** — Generates a 24x14 GridDefinition SO with S-path and tower slots
- **Bake Meshes into Prefabs** — Assigns Tetrahedron/Cube/Sphere meshes + Default-Lit material to enemy/tower/projectile prefabs (uses `GetComponentInChildren`)
- **Fix Tower Prefab References** — Links FirePoint, ModuleRegistry on tower prefab
- **Assign Default Material to Prefabs** — Sets Unity's default URP/Lit material on all gameplay prefabs (uses `GetComponentInChildren`)

## Mod Slots System (feature/mod-slots-system branch)

New projectile-building system replacing the node editor. See `docs/GAME_DESIGN.md` for full design.

### Architecture: Pipeline + Flyweight

Towers fire automatically. The player builds the **projectile** via a chain of mods in slots. The system uses a **Pipeline pattern** where each mod is an `IModStage` that runs in a specific phase.

```
Tower fires → PipelineCompiler.Compile(slots) → ModPipeline + ModContext
→ ModProjectile.Initialize(pipeline, ctx) → Configure phase runs (Split = multi-spawn)
→ Each frame: OnUpdate phase (homing, pulse, delay) → Move → Collision
→ On hit: OnHit phase (damage, elements, wide, events) → PostHit phase (pierce, bounce, leech)
→ DrainSpawns (sub-projectiles from events)
```

### Pipeline Framework (`Assets/Scripts/Mods/Pipeline/`)

| File | Role |
|------|------|
| `IModStage.cs` | Interface: `Phase`, `Execute(ref ModContext)`, `Clone()` |
| `StagePhase.cs` | Enum: Configure, OnUpdate, OnHit, PostHit, OnExpire |
| `ModContext.cs` | Mutable struct flowing through pipeline (position, damage, tags, spawns) |
| `ModTags.cs` | `[Flags]` enum — one bit per mod type for fast synergy checks |
| `SpawnRequest.cs` | Struct for sub-projectile spawn requests pushed by event stages |
| `ModPipeline.cs` | Ordered list of stages, grouped by phase. `RunPhase`, `Clone`, `AddStage` |
| `PipelineCompiler.cs` | Compiles `List<ModSlotData>` → `ModPipeline` + `ModContext`. Handles event splitting, synergy application, stage ordering |

### Stages (`Assets/Scripts/Mods/Pipeline/Stages/`)

Each mod = 1 file, 20-40 lines, implements `IModStage`. Adding a new mod = creating 1 file + 1 enum value.

**Behavior:** HomingStage, PierceStage, BounceStage, SplitStage, HeavyStage, SwiftStage, WideStage, ImpactFeedbackStage
**Elemental:** BurnStage, FrostStage, ShockStage, VoidStage, LeechStage
**Events:** OnHitEventStage, OnKillEventStage, OnEndEventStage, OnPierceEventStage, OnBounceEventStage, OnPulseEventStage, OnDelayEventStage, ConditionalEventStage, OnOverkillEventStage

### Projectile (`Assets/Scripts/Mods/ModProjectile.cs`)

Thin MonoBehaviour (~150 lines). Holds pipeline + context. Delegates all behavior to stages. Movement in XZ only at Y=0.5, WarpFollower handles grid deformation. Collision via EnemyRegistry sweep (no Physics).

### Key files

- `Assets/Scripts/Mods/ModType.cs` — All mod + event enums with extension methods
- `Assets/Scripts/Mods/ModSlotData.cs` — Serializable slot data
- `Assets/Scripts/Mods/SynergyDef.cs` — Adjacency synergy table
- `Assets/Scripts/Mods/ModSlotExecutor.cs` — Tower component: targeting + fire timer + pipeline compilation
- `Assets/Scripts/Mods/ModProjectile.cs` — Thin projectile MonoBehaviour
- `Assets/Scripts/Enemies/EnemyRegistry.cs` — Static enemy registry for O(n) targeting

### Targeting (no more Zone nodes)
Simple `TargetingMode` enum on tower: First, Nearest, Strongest, Weakest, Last. Uses `EnemyRegistry` for O(n) lookups.

### Chain evaluation rule
Mods before an Event = main projectile traits (Configure/OnUpdate/OnHit/PostHit stages). Mods after an Event = sub-projectile stages (in a sub-ModPipeline held by the event stage). Events push SpawnRequests that ModProjectile drains after each hit.

## Removed Systems

The following systems have been intentionally removed:
- **Threat system** (`ThreatSource`, `ThreatCalculator`, `ThreatCalculatorConfig`, `ThreatScenarioWindow`, `EnemyTargetRegistry`, `DefaultThreatConfig.asset`) — enemies no longer evaluate threats or chase towers
- **Enemy combat AI** (chase, attack, de-aggro states) — enemies follow routes only, classic TD style
- **DamageInfo.SourceId** — no longer needed without threat tracking

## Git Workflow (Gitflow)

- **`main`** — Production-ready. Protected, never commit directly.
- **`dev`** — Integration branch. All feature branches merge here.
- **`feature/<name>`** — Branch from `dev`, merge back via PR (squash merge).
- **`release/<version>`** — Branch from `dev`, merge into `main` and `dev`.
- **`hotfix/<name>`** — Branch from `main`, merge into `main` and `dev`.

PRs target `dev` by default. Delete feature branches after merge.
