# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Gridlock Portage is a **port of the Unity Gridlock game to C#/.NET 8 + Raylib-cs**. The original Unity project lives at `/Users/bastienokonski/Documents/gridlock` — refer to its `CLAUDE.md` for the full game design, architecture, and mechanics.

The game is an **isometric grid-based Tower Defense** with a neon/Geometry Wars aesthetic. The player defends an objective against enemy waves by placing towers and configuring them via a mod slot system.

## Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8 (C#) |
| Rendering | Raylib-cs 7.0.2 |
| Debug UI | rlImgui-cs 3.2.0 (ImGui.NET) |
| Shaders | GLSL 330 |
| Audio | Raylib audio API |

## Project Structure

```
src/Gridlock/           — Main C# source
  Program.cs            — Entry point (Raylib window + game loop)
  Core/                 — GameLoop (partial class split), GameManager, GameState, ServiceLocator
    GameLoop.cs          — Fields, Init, RunFrame, Update, FixedUpdate, Shutdown, data helpers
    GameLoop.Input.cs    — HandleGlobalInput, HandlePlacementInput (tower hover/selection)
    GameLoop.Events.cs   — Game event handlers (tower placed, enemy killed, wave cleared, etc.)
    GameLoop.Combat.cs   — Projectile registration, destruction VFX, trail management
    GameLoop.Rendering.cs— All 3D rendering (towers, enemies, projectiles, pickups, grid, HUD)
    BenchmarkRunner.cs   — Benchmark setup (tower placement, wave start) — no GameLoop internals
  Grid/                 — Grid system (port of Unity GridManager/GridVisual/GridWarp)
  Camera/               — Isometric camera
  Combat/               — Damage, SpatialHash for collision queries
  Enemies/              — Enemy system, EnemyPool for object recycling
  Towers/               — Tower data, placement
  Mods/                 — Mod slot pipeline, projectile lifecycle
    ModProjectile.cs     — Full projectile lifecycle: move, collide, hit, sub-projectile spawning
    ModSlotExecutor.cs   — Tower firing logic, target selection, pipeline compilation
    Pipeline/            — ModPipeline, ModContext, PipelineCompiler, StagePhase, SpawnRequest
    Pipeline/Stages/     — One file per effect (HeavyStage, SplitStage, BurnStage, etc.)
  Visual/               — Juice, effects, warp followers
  Rendering/            — Render pipeline, shaders, bloom, LineBatch (batched rlgl line renderer)
  UI/                   — ImGui-based UI
  Input/                — Input handling
  Loot/                 — Pickup system
  Audio/                — Audio management
resources/              — Runtime assets
  shaders/glsl330/      — GLSL fragment/vertex shaders
  audio/                — Sound effects
  data/                 — JSON data files (levels, enemies, waves)
```

## Build & Run

```bash
cd src/Gridlock
dotnet build
dotnet run
```

To publish a self-contained build:
```bash
dotnet publish -c Release -o ../../publish
```

## Profiling & Benchmarks

```bash
cd src/Gridlock

# Benchmark: no VSync, 8 towers near spawn with Split+Pierce, output report
dotnet run -- --benchmark --frames 1200

# Longer benchmark (30s of combat)
dotnet run -- --benchmark --frames 1800

# Profile with CSV export (with VSync, manual play)
dotnet run -- --profile

# Screenshot mode: auto-start, capture at frame 120
dotnet run -- --screenshot
```

Benchmark setup lives in `BenchmarkRunner.cs` — places 8 towers on TowerSlot cells sorted by distance to spawn, applies an aggressive mod preset (Heavy+Split+Swift+Pierce), then starts wave 1.

Output files (gitignored):
- `profile.csv` — per-frame timing data for each profiled section
- `profile_report.txt` — summary with avg/P50/P95/P99/max frame times
- `screenshot.png` — in-game capture

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. Remove comments that restate what the code does.
- **All gameplay values tunable** — expose as fields/config, no magic numbers.
- **Data-driven** — enemy definitions, level layouts, wave configs loaded from JSON in `resources/data/`.

## Projectile Architecture

The projectile system mirrors Unity's architecture as closely as possible:

- **`ModProjectile.DrainSpawns()`** creates sub-projectiles **inline** (same as Unity's `Instantiate` pattern). Sub-projectiles inherit tags, pierce, and bounce counts from their sub-pipeline's `AccumulatedTags`.
- **`ModProjectile.OnProjectileCreated`** — static callback set by GameLoop to register new projectiles (add to list + create trail). This is the Raylib equivalent of Unity's `Instantiate()` putting objects in the scene.
- **No spawn buffer** — sub-projectiles are registered immediately via the static callback during `DrainSpawns()`, so they enter the active list in the same frame.
- **Pipeline phases**: Configure → OnUpdate → OnHit → PostHit → OnExpire. Each stage is a separate file in `Mods/Pipeline/Stages/`.
- **Context cloning**: `CloneForSub()` preserves `HitInstances` (prevents re-hitting same enemies), resets tags/pierce/bounce (restored from sub-pipeline's `AccumulatedTags`).

When porting new mod stages from Unity, keep the same phase and execution order. Sub-projectile spawning must set `subCtx.Tags = subPipeline.AccumulatedTags` and restore pierce/bounce from those tags.

## Performance Patterns

The rendering and combat systems use several low-level patterns for performance:

- **LineBatch** (`Rendering/LineBatch.cs`) — Batched line renderer using `Rlgl` directly. Accumulates line segments (position + color) and flushes them in a single `Rlgl.Begin(GL_LINES)` / `Rlgl.End()` call. Used by `DrawTowers()` and `DrawEnemies()` instead of individual `Raylib.DrawLine3D` / `DrawCubeWires` calls. Includes geometry helpers: `CubeWires`, `OctahedronWires`, `PyramidWires`, `PyramidThick`. Note: for low-count line rendering (trails), `DrawLine3D` outperforms `LineBatch` due to less per-line overhead.
- **SpatialHash** (`Combat/SpatialHash.cs`) — Grid-based spatial hashing for projectile-enemy collision. Rebuilt each `FixedUpdate` via `EnemyRegistry.RebuildSpatial()`. Cell lists are pooled to avoid per-frame allocations. `ModProjectile.SweepCollision()` queries the hash instead of iterating all enemies.
- **EnemyPool** (`Enemies/EnemyPool.cs`) — Object pool for `Enemy` instances. `Enemy.Reset()` recycles an enemy with a new `EntityId`, fresh health, and cleared events/status effects. Eliminates GC pressure from spawn/death cycles.
- **Projectile draw cache** — `DrawProjectiles()` iterates once, caching position/color/radius in a `ProjectileDrawData[]` array, then replays the cache for the additive glow pass.
- **Swap-and-pop removal** — `EnemyRegistry.Unregister()` and `EnemySpawner.ProcessRemovals()` use swap-with-last + `RemoveAt(last)` instead of `List.Remove()` to avoid O(n) shifts.
- **Cached enum arrays** — `Enum.GetValues<T>()` allocates a new array per call. UI code (`ModSlotPanel`) caches these as `static readonly` arrays to avoid per-frame allocations.
- **Bitwise circular buffer** — `TrailSystem` uses `& (MaxPoints - 1)` instead of `% MaxPoints` for power-of-2 buffer wrapping. Trail rendering also uses `invDuration` (multiply) instead of per-segment division and skips invisible segments (`alpha < 2`).
- **Pre-computed loot tables** — `LootTable` builds a `Dictionary<Rarity, ModType[]>` at init instead of allocating a `List` + calling `Enum.GetValues` per loot roll.
- **Early-exit dead enemies** — `Enemy.Update()` checks `Health.PendingRemoval` before ticking `Health`/`StatusEffects`, skipping work on dying enemies.

When adding new rendered entities, prefer `LineBatch` over individual `Raylib.DrawLine3D` calls for 100+ lines. When adding new collision participants, integrate with `SpatialHash`.

## Porting Reference

The original Unity project at `/Users/bastienokonski/Documents/gridlock` is the reference implementation. Key mappings:

| Unity Concept | Raylib Port |
|---------------|-------------|
| MonoBehaviour lifecycle | GameLoop manual update |
| `Instantiate(gameObject)` | `new ModProjectile()` + `OnProjectileCreated` static callback |
| ScriptableObject | JSON data files in `resources/data/` |
| Unity Shader (HLSL) | GLSL 330 shaders in `resources/shaders/` |
| URP Post-Processing | Custom Raylib render pipeline |
| UI Toolkit (UXML/USS) | ImGui via rlImgui-cs |
| New Input System | Raylib input API |
| ServiceLocator | Same pattern, static dictionary |
| `Time.deltaTime` | Explicit `float dt` parameter (FixedUpdate at 60Hz) |
| `GetComponent<T>()` | Direct property access on `ITargetable` (e.g. `.StatusEffects`, `.Damageable`) |

When porting a system from the Unity version, refer to the original C# scripts in `/Users/bastienokonski/Documents/gridlock/Assets/Scripts/` and adapt them to the Raylib API.
