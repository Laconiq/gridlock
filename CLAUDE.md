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

## Code Style

- **No superfluous comments.** Only comment to explain *why* something non-obvious is done. The code should be self-documenting.
- **No Editor scripts.** Do not create scripts under `Assets/Scripts/Editor/`. Use ScriptableObjects, assets, or MCP tooling instead.
- **Module system uses `[SerializeReference]` + Odin.** Module definitions use `[SerializeReference, ListDrawerSettings, InlineProperty] List<T>` for concrete implementations (DarkTales pattern). The SO holds templates, `CreateInstance()` clones them at runtime. No string-based factory dispatch.
- **All gameplay values are `[SerializeField]`** for live Inspector tuning. No magic numbers in code.

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
