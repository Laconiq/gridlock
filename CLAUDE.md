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
| Camera | Cinemachine 3.1.6 |
| Level design | **LDtk** + LDtkToUnity (`com.cammin.ldtkunity`) |
| Scripting backend | IL2CPP |
| Inspector | Odin Inspector (Sirenix) |
| Juice/Feedback | Feel (MoreMountains) |
| Node Editor UI | Unity UI Toolkit (USS/UXML) |
| UI Font | Space Grotesk (variable weight) |

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. The code should be self-documenting.
- **No Editor scripts.** Do not create scripts under `Assets/Scripts/Editor/`. Use ScriptableObjects, assets, or MCP tooling instead.
- **Module system uses `[SerializeReference]` + Odin.** Module definitions use `[SerializeReference, ListDrawerSettings, InlineProperty] List<T>` for concrete implementations (DarkTales pattern). The SO holds templates, `CreateInstance()` clones them at runtime. No string-based factory dispatch.
- **All gameplay values are `[SerializeField]`** for live Inspector tuning. No magic numbers in code.
- **UI colors use design tokens.** USS uses `var(--token)` from `DesignTokens.uss`. C# uses `DesignConstants` static fields. Never hardcode hex colors.
- **UI components are reusable.** `ModuleElement`, `PopupPanel`, `CableRenderer` are shared components. Create display variants for previews.
- **Icons live in Resources only.** `Assets/Resources/UI/NodeEditor/Icons/` — no duplicates in `Assets/UI/`. Load via `Resources.Load<Texture2D>(DesignConstants.IconXxx)`.

## Architecture

### Networking authority

Server-authoritative model. All game state lives in `NetworkVariable` (server write, everyone read). Clients request changes via `ServerRpc`, server validates then commits. Key authority boundaries:

- **GameManager** — `NetworkVariable<GameState>` controls game flow (Lobby → Preparing → Wave → Intermission)
- **TowerChassis** — `NetworkVariable<FixedString4096Bytes>` stores serialized node graphs. Client sends `UpdateGraphRpc`, server validates size (<4KB) and deserializes before accepting
- **PlayerInventory** — `NetworkList<ModuleSlot>` synced to all. Mutations go through `ServerRpc`
- **EditorLockManager** — Server grants/denies locks atomically via RPCs

Rule: always check `IsServer` before writing state, `IsOwner` before processing input.

### Player 3C (Character, Camera, Controls)

Movement and camera are designed for a **nervous, snappy FPS feel** (Quake/Doom Eternal reference):

**PlayerController** — `CharacterController`-based movement:
- Asymmetric acceleration/deceleration (80/160 u/s²), base speed 8 m/s, no sprint
- Coyote time (0.1s) + jump buffer (0.1s)
- Air control at 10% of ground acceleration
- Exposes `CurrentSpeedNormalized`, `MoveInput`, `IsGrounded`, `OnLanded` event for feedback systems

**PlayerCamera** — Cinemachine 3 pipeline:
- `CinemachinePanTilt` for FPS look (sensitivity-driven, pitch clamped ±80°)
- Dynamic FOV: base 100° → 110° at full speed (`SmoothDamp`)
- Exposes `LookDelta` for weapon sway/camera roll
- `ApplyRecoil(pitch, yaw)` API for weapon kick

**Camera effects pipeline** (layered via Cinemachine extensions + MonoBehaviours):

| Component | Location | What it does |
|-----------|----------|-------------|
| `StrafeTiltExtension` | CinemachineCamera | Dutch angle ±3.5° on strafe input (Finalize stage) |
| `LookRollExtension` | CinemachineCamera | Dutch angle ±2° on mouse flicks (Finalize stage) |
| `HeadBobController` | CinemachineCamera | Drives `CinemachineBasicMultiChannelPerlin` amplitude by speed |
| `SpeedLinesController` | CinemachineCamera | Particle emission scales above 70% speed |
| `WeaponViewModel` | WeaponHolder | Sway (rotation lag) + position lag + procedural figure-8 bob |
| `WeaponFireFeedback` | WeaponHolder | Recoil punch + fire impulse shake + muzzle flash/light |
| `LandingImpactController` | PlayerPrefab | `CinemachineImpulseSource` fired on landing, scaled by fall speed |
| `MovementAudio` | PlayerPrefab | Footsteps (6 clips, anti-repeat), wind loop, landing thud, jump whoosh |
| `DynamicCrosshair` | UI Crosshair | 4 lines spread by movement/air state |
| `HitFeedback` | UI HUD | Hit markers (scale punch + fade) + kill flash |

**Adding new camera effects:** Create a `CinemachineExtension` for pipeline effects (use `PostPipelineStageCallback` at `Finalize` stage), or a plain `MonoBehaviour` for non-pipeline effects. Place in `Assets/Scripts/Player/CameraEffects/`.

### Player weapon system

Players configure their FPS weapon via the same node editor as towers (Tab to open). Two fixed trigger nodes (OnLeftClick / OnRightClick) are pre-placed and locked.

```
PlayerWeaponEditorController (Tab toggle, gated by InputEnabled)
  → NodeEditorScreen.Open(chassis, inventory)
  → Fixed nodes injected (isFixed=true, can't be deleted)
  → Save → PlayerWeaponChassis.SetNodeGraph() → server syncs
  → PlayerWeaponExecutor.RebuildFromGraph() categorizes chains:
    - leftClickChains (OnLeftClickTrigger)
    - rightClickChains (OnRightClickTrigger)
    - timerChains (OnTimerTrigger etc.)
  → Attack/AltAttack → FireLeftClickRpc() / FireRightClickRpc()
```

### Module system (core gameplay)

Towers are empty chassis configured via a visual node editor. Three module types chain together:

```
Trigger (WHEN) ──► Zone (WHERE/WHO) ──► Effect (WHAT)
                   Zone ──► Zone (serial chaining)
                   Effect ──► Effect (vertical chaining)
```

**Definition SOs** hold `[SerializeReference] List<T>` of `[Serializable]` runtime classes:
- `TriggerDefinition` → `List<TriggerInstance>` (e.g. OnTimerTrigger, OnEnemyEnterRangeTrigger, OnLeftClickTrigger, OnRightClickTrigger)
- `ZoneDefinition` → `List<ZoneInstance>` (e.g. NearestEnemyZone, AllEnemiesInRangeZone)
- `EffectDefinition` → `List<EffectInstance>` (e.g. ProjectileEffect, HitscanEffect, SlowEffect, DotEffect)

**Execution pipeline** (server only, in `TowerExecutor` / `PlayerWeaponExecutor`):
1. `RebuildFromGraph(NodeGraphData)` walks the graph from Trigger nodes
2. For each Trigger → follows connections to Zones → collects Effects
3. Builds `TriggerChain → ZoneChain → List<EffectInstance>` tree (max depth 8)
4. Each frame, triggers tick. On fire → zones select targets → effects execute on targets

### Node editor UI (UI Toolkit)

The node editor uses **Unity UI Toolkit** (not legacy Canvas/UGUI). All UI is built with USS/UXML + C# VisualElements.

**Design system:**
- `Assets/UI/NodeEditor/DesignTokens.uss` — Single source of truth for all colors, spacing, typography, transitions (CSS custom properties via `var()`)
- `Assets/UI/NodeEditor/NodeEditor.uss` — All component styles, references tokens only
- `Assets/Scripts/NodeEditor/UI/DesignConstants.cs` — C# mirror of tokens for runtime code (port colors, icon paths, node dimensions)

**Component architecture (React-like):**
```
ModuleElement : VisualElement (base component)
├── TriggerModuleElement  — orange accent, OUT port only
├── ZoneModuleElement     — cyan accent, IN/OUT + vertical port
└── EffectModuleElement   — green accent, vertical ports + status bar

Factory methods:
  ModuleElement.Create(name, category)        → interactive (for NodeWidget)
  ModuleElement.CreateDisplay(name, category)  → display-only (for docs, previews)
```

**Reusable UI components:**
- `PopupPanel` — modal overlay with pages, dot navigation, prev/next buttons
- `CableRenderer : VisualElement` — draws bezier cables via `Painter2D` (used by both canvas connections and doc diagrams)
- `ConnectionLayer : VisualElement` — canvas connection layer, repaints on node move

**Key UI files:**
```
Assets/UI/NodeEditor/
├── DesignTokens.uss          — design tokens (colors, spacing, fonts)
├── NodeEditor.uss            — component styles (uses var() tokens)
├── NodeEditorScreen.uxml     — main layout (top bar, sidebar, canvas, bottom bar)
└── Fonts/SpaceGrotesk-Variable.ttf

Assets/Resources/UI/NodeEditor/Icons/
├── icon_trigger.png          — Material Symbol: bolt
├── icon_zone.png             — Material Symbol: settings_overscan
├── icon_effect.png           — Material Symbol: auto_awesome
└── icon_help.png             — Material Symbol: help

Assets/Scripts/NodeEditor/UI/
├── DesignConstants.cs         — C# design tokens (colors, dimensions, icon paths)
├── ModuleElement.cs           — base module component + 3 variants
├── NodeWidget.cs              — interactive node (wraps ModuleElement + PortWidgets)
├── PortWidget.cs              — port with connection drag
├── ConnectionLayer.cs         — canvas bezier connections (Painter2D)
├── CableRenderer.cs           — reusable bezier cable drawing
├── NodeEditorCanvas.cs        — canvas pan, node/connection CRUD, minimap, drag overlay
├── NodeEditorScreen.cs        — main controller (UIDocument, open/close/save)
├── ModulePalette.cs           — sidebar category tabs + item list
├── ModulePaletteItem.cs       — draggable palette item (creates node on drag)
├── PopupPanel.cs              — reusable popup with pages + navigation
└── DocumentationContent.cs    — builds 4-page doc popup content
```

**Node drag behavior:**
- Drag from palette: node created immediately at cursor with spawn animation (scan-line expand)
- Drag existing node: reparented to overlay layer (above sidebar), connections follow in real-time
- Drop on sidebar: despawn animation (collapse to line) + node removed, module returned to inventory
- Fixed nodes (weapon triggers) cannot be deleted

**Adding new design tokens:** Add USS variable in `DesignTokens.uss`, add C# constant in `DesignConstants.cs`, use `var(--token)` in USS and `DesignConstants.X` in C#.

### Node editor data flow

```
Player saves graph → NodeEditorScreen.SaveGraph()
  → chassis.SetNodeGraph(graph)
    → Client: sends UpdateGraphRpc(json) → Server validates → NetworkVariable updated
    → Server: writes NetworkVariable directly
  → OnGraphChanged() → Executor.RebuildFromGraph()
  → PlayerInventory adjusted (module count delta, fixed nodes excluded)
```

Graph serialized as JSON via `GraphSerializer` (JsonUtility), stored in `FixedString4096Bytes`.

### Interaction flow

```
PlayerInteraction (raycast) → IInteractable found (TowerInteractable)
  → Interact() → EditorLockManager.RequestLockRpc()
    → Server grants/denies
  → OnLockGranted → NodeEditorController → NodeEditorScreen.Open(chassis, inventory)
    → Player controls disabled, cursor unlocked
  → Save & Close → graph saved, lock released, controls re-enabled
```

### ServiceLocator

Static dictionary for global singletons. Registered: `GameManager`, `NetworkBootstrap`, `EditorLockManager`.

```csharp
ServiceLocator.Register<T>(instance);  // in Awake
ServiceLocator.Get<T>();               // anywhere
ServiceLocator.Unregister<T>();        // in OnDestroy
```

### Key interfaces

| Interface | Contract | Implementors |
|-----------|----------|-------------|
| `IChassis` | Tower/weapon config container (get/set graph, fire point, max triggers, base range) | TowerChassis, PlayerWeaponChassis |
| `IInteractable` | Interaction prompt + permission + action | TowerInteractable |
| `ITargetable` | Position, IsAlive, Transform for targeting | EnemyController |
| `IDamageable` | TakeDamage(DamageInfo) | EnemyHealth |

## Key Files

- `Controls.inputactions` / `Controls.cs` — Input bindings. **Do not edit Controls.cs** (auto-generated)
- `Assets/Scenes/GameScene.unity` — Main game scene (build index 0)
- `Assets/Scenes/3CTestScene.unity` — Movement debug scene (auto-hosts, no Relay/Lobby)
- `Assets/Data/Modules/` — Module definition SOs (Triggers/, Zones/, Effects/)
- `Assets/Data/ModuleRegistry.asset` — Registry referencing all module definitions
- `Assets/Data/DefaultLoadout.asset` — Starter module kit given to players on spawn
- `Assets/Data/Camera/HeadBobNoise.asset` — Cinemachine noise profile for head bob
- `Assets/Sounds/Footsteps/` — 6 footstep WAV variations (placeholder)
- `Assets/Sounds/Movement/` — Landing, wind loop, jump sounds (placeholder)
- `Assets/Scripts/Player/CameraEffects/` — All camera/weapon/audio feedback scripts
- `Assets/UI/NodeEditor/DesignTokens.uss` — **Design system tokens** (edit this to change theme)
- `Assets/Scripts/NodeEditor/UI/DesignConstants.cs` — **C# design constants** (must stay in sync with DesignTokens.uss)
- `Assets/UI/NodeEditor/NodeEditorScreen.uxml` — Node editor main layout
- `Assets/UI/NodeEditor/NodeEditorPanelSettings.asset` — UI Toolkit PanelSettings (1920×1080 ref, scale with screen)

### Player prefab component map (`Assets/Prefabs/Network/PlayerPrefab.prefab`)

```
PlayerPrefab (root)
├── PlayerController, NetworkObject, CharacterController, NetworkTransform
├── PlayerInteraction, PlayerInventory
├── PlayerWeaponChassis, PlayerWeaponExecutor, PlayerWeaponEditorController
├── CinemachineImpulseSource (landing), LandingImpactController
├── MovementAudio, HitFeedback
├── PlayerBody (mesh)
├── CameraRig
│   ├── PlayerCamera (look + FOV)
│   └── PlayerCinemachineCamera
│       ├── CinemachineCamera, CinemachinePanTilt
│       ├── CinemachineBasicMultiChannelPerlin (head bob)
│       ├── CinemachineImpulseListener
│       ├── StrafeTiltExtension, LookRollExtension
│       ├── HeadBobController, SpeedLinesController
│       └── SpeedLinesParticles
│   └── WeaponHolder
│       ├── WeaponViewModel (sway + lag + bob)
│       ├── WeaponFireFeedback (recoil + shake + flash)
│       ├── CinemachineImpulseSource (fire shake)
│       ├── WeaponModel (mesh)
│       └── FirePoint (ParticleSystem + Light)
├── FootstepAudio (AudioSource)
├── WindAudio (AudioSource, loop)
└── ImpactAudio (AudioSource)
```

## Git Workflow (Gitflow)

- **`main`** — Production-ready. Protected, never commit directly.
- **`dev`** — Integration branch. All feature branches merge here.
- **`feature/<name>`** — Branch from `dev`, merge back via PR (squash merge).
- **`release/<version>`** — Branch from `dev`, merge into `main` and `dev`.
- **`hotfix/<name>`** — Branch from `main`, merge into `main` and `dev`.

PRs target `dev` by default. Delete feature branches after merge.
