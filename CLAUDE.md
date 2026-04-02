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
    GameLoop.Combat.cs   — Projectile lifecycle (spawn, destroy, sub-projectiles, trails)
    GameLoop.Rendering.cs— All 3D rendering (towers, enemies, projectiles, pickups, grid, HUD)
  Grid/                 — Grid system (port of Unity GridManager/GridVisual/GridWarp)
  Camera/               — Isometric camera
  Combat/               — Damage, projectiles
  Enemies/              — Enemy system
  Towers/               — Tower chassis, placement
  Mods/                 — Mod slot pipeline
  Visual/               — Juice, effects, warp followers
  Rendering/            — Render pipeline, shaders, bloom
  UI/                   — ImGui-based UI
  Input/                — Input handling
  Loot/                 — Pickup system
  Data/                 — Data loading
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

# Benchmark: no VSync, auto-place towers, start wave, output report
dotnet run -- --benchmark --frames 1200

# Longer benchmark (30s of combat)
dotnet run -- --benchmark --frames 3600

# Profile with CSV export (with VSync, manual play)
dotnet run -- --profile

# Screenshot mode: auto-start, capture at frame 120
dotnet run -- --screenshot
```

Output files (gitignored):
- `profile.csv` — per-frame timing data for each profiled section
- `profile_report.txt` — summary with avg/P50/P95/P99/max frame times
- `screenshot.png` — in-game capture

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. Remove comments that restate what the code does.
- **All gameplay values tunable** — expose as fields/config, no magic numbers.
- **Data-driven** — enemy definitions, level layouts, wave configs loaded from JSON in `resources/data/`.

## Porting Reference

The original Unity project at `/Users/bastienokonski/Documents/gridlock` is the reference implementation. Key mappings:

| Unity Concept | Raylib Port |
|---------------|-------------|
| MonoBehaviour lifecycle | GameLoop manual update |
| ScriptableObject | JSON data files in `resources/data/` |
| Unity Shader (HLSL) | GLSL 330 shaders in `resources/shaders/` |
| URP Post-Processing | Custom Raylib render pipeline |
| UI Toolkit (UXML/USS) | ImGui via rlImgui-cs |
| New Input System | Raylib input API |
| ServiceLocator | Same pattern, static dictionary |

When porting a system from the Unity version, refer to the original C# scripts in `/Users/bastienokonski/Documents/gridlock/Assets/Scripts/` and adapt them to the Raylib API.
