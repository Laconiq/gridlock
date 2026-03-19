# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AIWE is a **cooperative FPS Tower Defense** (2-4 players) built in Unity 6 (6000.3.10f1) with URP. Players defend a zone against enemy waves by combining FPS combat with modular configurable defenses via a visual node editor. See `docs/GAME_DESIGN.md` for the full GDD and `docs/TECHNICAL.md` for detailed technical architecture.

## Stack & Tools

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6 (6000.3.10f1) |
| Rendering | URP 17.3.0 |
| Input | New Input System 1.18.0 |
| Networking | Netcode for GameObjects (NGO) 2.10.0 |
| Multiplayer services | Unity Relay + Lobby |
| AI navigation | AI Navigation 2.0.12 |
| Level design | **LDtk** + LDtkToUnity (`com.cammin.ldtkunity`) |
| Cinematics | Timeline 1.8.10 |
| Scripting backend | IL2CPP |
| Inspector | Odin Inspector (Sirenix) |
| Juice/Feedback | Feel (MoreMountains) |

## Architecture

### Folder structure

```
Assets/
├── Animations/          # Animation clips, Animator Controllers
├── Audio/               # SFX and music
├── LDtk/                # LDtk project files and tilesets
├── Materials/           # URP materials
├── Meshes/              # 3D models (.fbx, .obj)
├── Prefabs/             # Reusable prefabs (Chassis, Modules, Enemies, Entities)
├── Scenes/              # Unity scenes
├── Scripts/             # C# code
│   ├── Controls.inputactions / Controls.cs (auto-generated — DO NOT EDIT)
│   ├── Core/            # GameManager, WaveManager
│   ├── Player/          # FPS controller, weapons, interactions
│   ├── Towers/          # Chassis + module system
│   ├── NodeEditor/      # Visual node editor UI
│   ├── Enemies/         # AI, pathfinding, spawner
│   ├── Network/         # NGO networking logic
│   └── UI/              # HUD, menus
├── Settings/            # URP configs (PC_RPAsset, PC_Renderer)
├── Textures/            # Textures and sprites
└── Plugins/             # Third-party (Odin, Feel, editor tools)
```

### Key systems

- **Rendering:** URP — `PC_RPAsset` for desktop. Config in `Assets/Settings/`.
- **Input:** `Assets/Scripts/Controls.inputactions` defines all bindings. The C# wrapper regenerates automatically — do not edit `Controls.cs`.
- **Scene:** `Assets/Scenes/GameScene.unity` (build index 0).
- **Level Design:** LDtk files in `Assets/LDtk/`. Imported via LDtkToUnity ScriptedImporter. Tile layers → Tilemaps, Entity layers → Prefab instances.
- **Networking:** Client-server via NGO. Unity Relay for NAT traversal, Lobby for matchmaking.

### Module system (core gameplay)

Towers are empty chassis configured via a visual node editor with 3 module types chained together:

```
Trigger (WHEN) ──► Zone (WHERE/WHO) ──► Effect (WHAT)
```

- **Trigger**: event that starts execution (On Enemy Enter, On Timer, On Kill…)
- **Zone**: target selection (Nearest Enemy, All In Range, Self…)
- **Effect**: concrete action (Projectile, Hitscan, Slow, Knockback…)

## Input Actions

**Player:** Move, Look, Attack, Interact (hold), Crouch, Jump, Sprint, Previous, Next
**UI:** Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevicePosition, TrackedDeviceOrientation
**Control schemes:** Keyboard & Mouse, Gamepad, Touch, Joystick, XR

## Git Workflow (Gitflow)

- **`main`** — Production-ready. Only receives merges from `release/*` and `hotfix/*`. Protected, never commit directly.
- **`dev`** — Integration branch (default). All feature branches merge here.
- **`feature/<name>`** — New features. Branch from `dev`, merge back via PR.
- **`release/<version>`** — Release prep. Branch from `dev`, merge into `main` and `dev`.
- **`hotfix/<name>`** — Urgent fixes. Branch from `main`, merge into `main` and `dev`.

### Conventions

- `feature/player-movement`, `release/0.1.0`, `hotfix/fix-crash-on-load`
- All work on `feature/*` branches, never directly on `dev` or `main`
- PRs target `dev` by default
- Squash merge for features, merge commit for releases/hotfixes
- Delete feature branches after merge

## Code Style

- **No superfluous comments.** Do not add comments that restate what the code does. Only comment to explain *why* something non-obvious is done. The code should be self-documenting.
- **No Editor scripts.** Do not create scripts under `Assets/Scripts/Editor/`. Use ScriptableObjects, assets, or MCP tooling instead.
- **Module system uses `[SerializeReference]` + Odin.** Module definitions (Trigger/Zone/Effect) use `[SerializeReference, ListDrawerSettings, InlineProperty] List<T>` for concrete implementations, following the DarkTales pattern. No string-based factory dispatch — the SO holds the template, `CreateInstance()` clones it.

## Development

- Open in Unity 6 (version 6000.3.10f1)
- Solution file: `AIWE.sln`
- C# scripts go in `Assets/Scripts/` in the appropriate subfolder
- Edit input bindings via `Controls.inputactions` in Unity (wrapper auto-regenerates)
- LDtk levels: edit in LDtk app, auto-reimported on save
