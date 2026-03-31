# Codebase Review — 2026-03-31

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 5 |
| MEDIUM | 21 |
| MINOR | 13 |

---

## CRITICAL

### 1. VoidStage runs after damage is already dealt — does nothing useful
**Files:** `Mods/Pipeline/Stages/VoidStage.cs:15-21`
VoidStage has `Phase => StagePhase.OnHit`, but `ProcessHit` calls `TakeDamage()` BEFORE `RunPhase(OnHit)`. The %HP damage never applies. Worse, it overwrites `ctx.Damage` with the tiny void value, poisoning downstream stages (Leech, Wide, Shock) that read `ctx.Damage`.

**Fix:** VoidStage should either modify damage BEFORE TakeDamage (new pre-hit phase?) or apply its own TakeDamage call separately and NOT overwrite ctx.Damage.

### 2. StatusEffectManager: TickInterval=0 causes infinite DPS
**File:** `Combat/StatusEffectManager.cs:47-68`
DoT tick logic has no guard for `TickInterval <= 0`. A default-constructed `StatusEffectData` has `TickInterval = 0f`, which means `TakeDamage` fires every single frame. One misconfigured BurnStage = instant kill.

**Fix:** Add `if (effect.Data.TickInterval <= 0f) continue;` before the tick check.

### 3. GameJuice: screen shake drifts camera permanently
**File:** `Visual/GameJuice.cs:160-166`
Shake adds `Random.insideUnitSphere * magnitude` to camera position each frame but never stores/restores the original position. Each frame's offset accumulates. Camera gradually drifts within clamp bounds.

**Fix:** Store pre-shake position, apply random offset, restore on next frame before applying new offset.

### 4. GameJuice: freeze frame can permanently zero Time.timeScale
**File:** `Visual/GameJuice.cs:141-147, 152-157`
`FreezeFrame` sets `Time.timeScale = 0`. If GameJuice is destroyed during a freeze (scene transition), `OnDestroy` doesn't restore timeScale. Game stays frozen.

**Fix:** Add `Time.timeScale = 1f;` to `OnDestroy`.

### 5. EnemyController: objective damage + death race condition
**File:** `Enemies/EnemyController.cs:192`
If a DoT kills an enemy on the same frame it reaches the objective, both `Die()` and `NotifyReachedObjective()` fire. The objective takes damage from an already-dead enemy. No `_reachedObjective` guard exists.

**Fix:** Add `private bool _reachedObjective;` guard, check it before dealing objective damage.

---

## MEDIUM

### Pipeline

**6. OnPulse spawns never drained during Update** — `ModProjectile.cs:81-88`
OnPulseEventStage adds SpawnRequests during OnUpdate, but if `Consumed` is false, Update proceeds to movement/collision without calling `DrainSpawns()`. Pulse subs are delayed until the next hit/expire.
**Fix:** Add `DrainSpawns()` after `RunPhase(OnUpdate)` if spawn requests exist.

**7. HomingStage checks tags for Missile instead of synergy list** — `HomingStage.cs:27`
Any Homing+Swift combo activates perfect homing, even on sub-projectiles that inherit tags without the synergy. Should check `ctx.Synergies.Contains(SynergyEffect.Missile)`.

**8. Tesla synergy never implemented** — `ShockStage.cs`
ShockStage never checks for `SynergyEffect.Tesla`. chainCount stays at 1 regardless.

**9. Vampire synergy never implemented** — `LeechStage.cs`
LeechStage never checks for `SynergyEffect.Vampire`. leechPercent stays at 12% regardless.

**10. Napalm/Avalanche synergies detected but no stage acts on them**
`SynergyEffect.Napalm` and `SynergyEffect.Avalanche` are in the synergy table but no stage checks for them. Players see synergy activation UI but nothing happens.

**11. WideStage doesn't add AOE-hit enemies to HitInstances** — `WideStage.cs:23-33`
Pierce/Bounce can re-trigger Wide on the same enemies. Should add hit IDs.

**12. WideStage missing null check on entry.Controller** — `WideStage.cs:25`
Other iteration sites check `entry.Controller == null` first. WideStage skips this.

**13. ConditionalEventStage uses GetComponent instead of GetComponentInParent** — `ConditionalEventStage.cs:37-39`
BurnStage/FrostStage/VoidStage use `GetComponentInParent<>()` but Conditional uses `GetComponent<>()`. If StatusEffectManager is on a parent, IfBurning/IfFrozen/IfLow never trigger.

**14. Synergy detection ignores event boundaries** — `PipelineCompiler.cs:25-39`
`[Heavy, OnHit, Heavy]` activates Railgun even though the two Heavy mods are on different projectiles.

### Enemies/Combat

**15. EnemyHitFeedback: material leak on OnEnable** — `EnemyHitFeedback.cs:43`
`.material` creates a new instance each OnEnable. Never cleaned up on OnDisable.

**16. EnemyRegistry: static list persists across editor play sessions** — `EnemyRegistry.cs:13`
If play mode is stopped abruptly, stale entries with destroyed refs remain. Causes NullRefs on next play.

**17. StatusEffectManager: DamageMultiplier computed but never consumed** — `StatusEffectManager.cs:13`
Weaken/DamageBoost effect types silently do nothing.

### Grid/Visual/Core

**18. TowerPlacementSystem: _placedTowers never cleared on reset** — `TowerPlacementSystem.cs:28`
After `GameManager.ResetGame()`, `RemainingTowers` reports 0. Player can't place towers. **Softlock.**

**19. GameManager.ResetGame doesn't reset GameStats** — `GameManager.cs:54-68`
Kill count carries over between games.

**20. GridVisual: mesh and material never destroyed** — `GridVisual.cs:46,51`
Procedural mesh and instance material leak on scene reload.

**21. ImpactFlash: material leak on every spawn** — `ImpactFlash.cs:39`
`new Material()` per hit, never destroyed. Rapid accumulation.

**22. SimpleGameBootstrap fires SetState(Preparing) twice on startup** — `SimpleGameBootstrap.cs:18-26`
GameManager.Start already sets Preparing. Bootstrap does it again, firing OnStateChanged twice.

**23. ServiceLocator: static dict persists across editor sessions** — `ServiceLocator.cs:9`
Same editor-persistence issue as EnemyRegistry.

### UI/Loot/Player

**34. PlayerInventory.debugStartCount active in production builds** — `Loot/PlayerInventory.cs:14,30`
`debugStartCount = 5` gives 5 of every mod type on Start with no `#if UNITY_EDITOR` guard. Ships to players.

**35. PlayerInteraction: no null check on PlayerInputProvider** — `Player/PlayerInteraction.cs:40`
Missing provider component → NullRef in Start, interaction never works.

**36. LootDropper: missing ModulePickup component on prefab silently gives wrong item** — `Loot/LootDropper.cs:29`
If pickup prefab lacks ModulePickup, Initialize is skipped, pickup has default enum value (wrong mod type).

---

## MINOR

**24.** `EnemyHealth.SetMaxHP` is dead code — identical to `SetInitialHP`, never called.
**25.** `EnemySpawner._routeManager` fetched but never used.
**26.** `EnemyAI._routeId` stored but never read after Setup.
**27.** `PathVisualizer._baseLineColor` declared but never used.
**28.** `VoxelPool._alive` array set but never read — dead code.
**29.** `ModPipeline.AddStage` rebuilds entire phase map on every add — O(n^2).
**30.** `SpawnRequest.RandomDirectionExcluding` exclusion zone is asymmetric (~1° error).
**31.** `DamageTextFloat` calls `Camera.main` per frame (expensive pre-2023).
**32.** `TopDownCamera.HandleZoom` applies zoom lerp twice per frame (also in ClampPosition).
**33.** `_fireTimer` in ModSlotExecutor grows unbounded when no target available.
**37.** `WaveStartUI.OnStateChanged` swapped prev/current args — semantic bug, harmless today.
**38.** `ModulePickup.GetMagnetTarget` calls `Camera.main` twice redundantly.
**39.** `InventoryPanel` subscription tracking has edge case race between Enable/Open/Disable.

---

## Recommended Fix Priority

### Immediate (gameplay-breaking)
1. **#18 TowerPlacementSystem softlock** — add cleanup in ResetGame
2. **#2 TickInterval=0 guard** — one line fix prevents infinite damage
3. **#3+#4 GameJuice camera drift + timeScale** — store/restore position, add OnDestroy reset
4. **#6 OnPulse drain** — add DrainSpawns after OnUpdate

### High (incorrect behavior)
5. **#1 VoidStage** — redesign to apply damage correctly
6. **#5 Objective damage race** — add _reachedObjective guard
7. **#13 ConditionalEventStage** — change to GetComponentInParent
8. **#11 WideStage HitInstances** — add hit tracking

### Medium (synergies/polish)
9. **#8-#10 Unimplemented synergies** — Tesla, Vampire, Napalm, Avalanche
10. **#7 HomingStage Missile check** — use synergy list
11. **#14 Synergy cross-event detection** — scope to same group

### Quick wins
9. **#34 debugStartCount** — wrap in `#if UNITY_EDITOR`
10. **#35 PlayerInteraction null check** — add null guard

### Low (leaks/editor)
11. **#15,#20,#21 Material/mesh leaks** — add cleanup
12. **#16,#23 Static persistence** — add [RuntimeInitializeOnLoadMethod] clear
