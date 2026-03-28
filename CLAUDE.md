# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AIWE is a **2D grid-based Tower Defense** built in Unity 6 (6000.3.10f1) with URP. Neon/vector aesthetic (Geometry Wars style). The player defends an objective against enemy waves by placing towers on a grid and configuring them via a visual node editor. Orthographic top-down camera. Enemies follow predefined paths on the grid. No 3D models — everything uses procedural geometric shapes (triangles, diamonds, circles) with custom glow/outline shaders + heavy Bloom.

## Stack & Tools

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6 (6000.3.10f1) |
| Rendering | URP 17.x |
| Input | New Input System 1.19.0 |
| Level design | Grid system (`GridDefinition` ScriptableObjects) |
| Visuals | Custom shaders (VectorGlow, VectorOutline, CyberGrid) + URP Bloom |
| Scripting backend | IL2CPP |
| Inspector | Odin Inspector (Sirenix) |
| Node Editor UI | Unity UI Toolkit (USS/UXML) |
| UI Font | Space Grotesk (variable weight) |

**No networking.** The project is single-player only. No Netcode, Relay, or Lobby packages.

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. The code should be self-documenting.
- **No custom Inspector scripts.** Do not create custom editors, property drawers, or Inspector scripts — use Odin attributes and ScriptableObjects instead. Exceptions: `#if UNITY_EDITOR`-guarded editor tools (e.g. `CreateTestGrid`) are allowed when they serve the pipeline.
- **Module system uses `[SerializeReference]` + Odin.** Module definitions use `[SerializeReference, ListDrawerSettings, InlineProperty] List<T>` for concrete implementations. The SO holds templates, `CreateInstance()` clones them at runtime. No string-based factory dispatch.
- **All gameplay values are `[SerializeField]`** for live Inspector tuning. No magic numbers in code.
- **UI colors use design tokens.** USS uses `var(--token)` from `DesignTokens.uss`. C# uses `DesignConstants` static fields. Never hardcode hex colors.
- **UI components are reusable.** `ModuleElement`, `PopupPanel`, `CableRenderer` are shared components. Create display variants for previews.
- **Icons live in Resources only.** `Assets/Resources/UI/NodeEditor/Icons/` — no duplicates in `Assets/UI/`. Load via `Resources.Load<Texture2D>(DesignConstants.IconXxx)`.

## Architecture

### Game flow (solo)

```
SimpleGameBootstrap.Start()
  → Instantiate PlayerPrefab (invisible command entity)
  → GameManager.SetState(Preparing)
  → Player clicks towers to configure them via NodeEditorScreen
  → Player clicks "Start Wave" (WaveStartUI)
  → GameManager.SetState(Wave)
  → Enemies spawn, follow routes, attack towers/objective
  → Enemies die → loot flies to inventory (magnet auto-collect)
  → Wave cleared → GameManager.SetState(Preparing)
  → Objective destroyed → GameManager.SetState(GameOver)
```

### Grid System

`GridDefinition` SO (`Assets/Scripts/Grid/GridDefinition.cs`):
- Defines grid dimensions (width, height, cellSize), cell types array, path definitions, objective HP
- Cell types: `Empty`, `Path`, `TowerSlot`, `Blocked`, `Spawn`, `Objective`
- Paths are ordered `List<Vector2Int>` waypoints on the grid

`GridManager` (`Assets/Scripts/Grid/GridManager.cs`):
- ServiceLocator singleton, converts grid↔world coordinates
- Provides routes, spawn positions, objective position
- Replaces both LDtk level linker and NavMesh-based routing

`TowerPlacementSystem` (`Assets/Scripts/Grid/TowerPlacementSystem.cs`):
- During Preparing state: mouse → grid coord → validate TowerSlot → place tower
- Grid-snapped preview with valid/invalid material feedback

### Camera (orthographic)

`TopDownCamera` (`Assets/Scripts/Camera/TopDownCamera.cs`):
- Orthographic camera looking straight down (90° X rotation)
- Pan: WASD + edge panning + middle mouse drag
- Zoom: scroll wheel (adjusts orthographicSize)
- Bounds clamping to grid area
- Works in XZ plane (all gameplay uses Vector3 with Y=0)

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

**Execution pipeline** (in `TowerExecutor`):
1. `RebuildFromGraph(NodeGraphData)` walks the graph from Trigger nodes
2. For each Trigger → follows connections to Zones → collects Effects
3. Builds `TriggerChain → ZoneChain → List<EffectInstance>` tree (max depth 8)
4. Each frame, triggers tick. On fire → zones select targets → effects execute on targets

### Tower interaction flow

```
PlayerInteraction (mouse raycast) → IInteractable found (TowerInteractable)
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

**Adding new design tokens:** Add USS variable in `DesignTokens.uss`, add C# constant in `DesignConstants.cs`, use `var(--token)` in USS and `DesignConstants.X` in C#.

### ServiceLocator

Static dictionary for global singletons. Registered: `GameManager`, `ObjectiveController`, `RouteManager`.

```csharp
ServiceLocator.Register<T>(instance);  // in Awake
ServiceLocator.Get<T>();               // anywhere
ServiceLocator.Unregister<T>();        // in OnDestroy
```

### Enemy AI system

State machine (`EnemyAI`) with threat-based aggro against towers.

**State machine:** `FollowRoute → ChaseTarget → Attack → ReturnToRoute`

**Threat evaluation** (in `ThreatCalculator`, called from `EnemyAI`):
- `ThreatSource` components on towers track recent DPS (exponential decay)
- Static `ThreatSource.All` registry (HashSet) for O(1) lookup
- Score = weighted average of distance, line-of-sight, DPS, crowd factor
- Config via `ThreatCalculatorConfig` ScriptableObject

**Pathing:**
- `RouteManager` delegates to `GridManager` for route data
- `EnemyController` uses simple waypoint lerp (no NavMesh), speed modulated by `StatusEffectManager`
- Enemies are procedural geometric shapes (`GeometricShape` component)

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
4. `TowerPlacementSystem` enables tower placement on `TowerSlot` cells during Preparing state
5. `EnemySpawner` spawns enemies at `Spawn` cells, `EnemyAI` follows routes to `Objective`

### Key interfaces

| Interface | Contract | Implementors |
|-----------|----------|-------------|
| `IChassis` | Tower config container (get/set graph, fire point, max triggers, base range) | TowerChassis |
| `IInteractable` | Interaction prompt + permission + action | TowerInteractable |
| `ITargetable` | Position, IsAlive, Transform for targeting | EnemyController |
| `IDamageable` | TakeDamage(DamageInfo) | EnemyHealth, ObjectiveController |

### Loot system

Enemies drop `ModulePickup` on death. Pickups have a magnet behavior: after a short delay, they fly toward the camera and auto-add to the player's `PlayerInventory`.

## Key Files

- `Controls.inputactions` / `Controls.cs` — Input bindings. **Do not edit Controls.cs** (auto-generated)
- `Assets/Scenes/GameScene.unity` — Main game scene (build index 0)
- `Assets/Data/Modules/` — Module definition SOs (Triggers/, Zones/, Effects/)
- `Assets/Data/ModuleRegistry.asset` — Registry referencing all module definitions
- `Assets/Data/DefaultLoadout.asset` — Starter module kit given to players on spawn
- `Assets/Scripts/Camera/TopDownCamera.cs` — Orthographic top-down camera controller
- `Assets/Scripts/Grid/GridDefinition.cs` — Grid level data (SO)
- `Assets/Scripts/Grid/GridManager.cs` — Grid runtime manager (ServiceLocator singleton)
- `Assets/Scripts/Grid/TowerPlacementSystem.cs` — Grid-snapped tower placement
- `Assets/Scripts/Grid/GridVisual.cs` — Ground plane with CyberGrid shader
- `Assets/Scripts/Visual/GeometricShape.cs` — Procedural mesh generator (Triangle, Diamond, Circle, etc.)
- `Assets/Scripts/Core/SimpleGameBootstrap.cs` — Solo game initialization
- `Assets/Scripts/Core/GameStats.cs` — Kill/wave tracking singleton
- `Assets/Data/Levels/TestGrid.asset` — Test level GridDefinition
- `Assets/UI/NodeEditor/DesignTokens.uss` — **Design system tokens** (edit this to change theme)
- `Assets/Scripts/NodeEditor/UI/DesignConstants.cs` — **C# design constants** (must stay in sync with DesignTokens.uss)
- `Assets/UI/NodeEditor/NodeEditorScreen.uxml` — Node editor main layout
- `Assets/Shaders/CyberGrid.shader` — Grid ground plane shader
- `Assets/Shaders/VectorGlow.shader` — Neon glow shader for enemies/projectiles
- `Assets/Shaders/VectorOutline.shader` — Outlined square shader for towers
- `Assets/Scripts/AI/ThreatCalculatorConfig` — Threat evaluation weights (ScriptableObject)

## Git Workflow (Gitflow)

- **`main`** — Production-ready. Protected, never commit directly.
- **`dev`** — Integration branch. All feature branches merge here.
- **`feature/<name>`** — Branch from `dev`, merge back via PR (squash merge).
- **`release/<version>`** — Branch from `dev`, merge into `main` and `dev`.
- **`hotfix/<name>`** — Branch from `main`, merge into `main` and `dev`.

PRs target `dev` by default. Delete feature branches after merge.
