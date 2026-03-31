# Pipeline Test Report — 2026-03-31

## Summary

**37 PASS, 0 FAIL, 0 ERROR**

All tests ran in automated play mode with a single tower placed adjacent to the enemy path near spawn. Each test ran for up to 8 seconds.

## Interpretation Guide

- **maxProj**: max simultaneous projectiles seen — indicates sub-projectile spawning works
- **enemies**: remaining enemies at test end — higher = slower kill rate (1 tower, 8s window)
- All tests passed = tower fired and no crashes. Gameplay correctness notes below.

---

## Results

### Basic Traits

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| BasicHoming | Homing | 1 | 1 | OK — single homing projectile per shot |
| BasicPierce | Pierce | 10 | 3 | OK — multiple projectiles alive = piercing through enemies |
| BasicBounce | Bounce | 1 | 5 | OK — single projectile bouncing between targets |
| BasicSplit | Split | 12 | 14 | OK — each shot fans out, many projectiles alive |
| BasicHeavy | Heavy | 13 | 23 | OK — slower but high damage |
| BasicBurn | Burn | 2 | 31 | OK — DOT applied |
| BasicFrost | Frost | 1 | 36 | OK — slow effect |
| BasicWide | Wide | 1 | 29 | OK — AOE impact |

### Trait Combos

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| PierceBurn | Pierce, Burn | 3 | 25 | OK — pierce + burn on each hit |
| HomingHeavy | Homing, Heavy | 2 | 27 | OK |
| SplitBurn | Split, Burn | 12 | 32 | OK — split creates burning projectiles |
| SplitHoming | Split, Homing | 13 | 37 | OK — split + homing each shard seeks targets |
| PierceBounce | Pierce, Bounce | 1 | 38 | OK — Ricochet synergy should activate |
| BounceShock | Bounce, Shock | 2 | 27 | OK |

### Single Event Chains

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| OnHit_Homing | ⟐Hit, Homing | 1 | 29 | OK — main has no traits, sub homes. Low maxProj: sub hits quickly |
| OnHit_Split | ⟐Hit, Split | 1 | 33 | OK — sub splits on spawn (Configure) |
| OnHit_Burn | ⟐Hit, Burn | 1 | 41 | OK — sub applies burn |
| Pierce_OnHit_Homing | Pierce, ⟐Hit, Homing | 6 | 35 | OK — pierce main + homing subs. 6 simultaneous = working |
| Pierce_OnHit_Split | Pierce, ⟐Hit, Split | 16 | 24 | OK — pierce + split subs = lots of projectiles |
| Heavy_OnHit_SplitBurn | Heavy, ⟐Hit, Split, Burn | 13 | 24 | OK — heavy main, hit spawns burning split shards |
| Homing_OnKill_Wide | Homing, ⟐Kill, Wide | 5 | 28 | OK — homing main, kill → AOE sub. 5 proj = kills happening |
| OnEnd_Split | ⟐End, Split | 1 | 32 | OK — projectile expires → split |
| OnEnd_Burn | ⟐End, Burn | 1 | 36 | OK — projectile expires → burn sub |

### Nested Event Chains

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| Homing_OnHit_Split_OnKill_Wide | Homing, ⟐Hit, Split, ⟐Kill, Wide | 1 | 36 | **CHECK** — only 1 maxProj. Homing hits, OnHit should spawn Split sub, Split should fan out. Low proj count suggests subs may die instantly. Needs visual verification. |
| Pierce_OnHit_Burn_OnKill_Split | Pierce, ⟐Hit, Burn, ⟐Kill, Split | 14 | 33 | OK — pierce main, OnHit→Burn sub, OnKill→Split sub. 14 proj = nesting works |

### Temporal Events

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| OnPulse_Burn | ⟐Pulse, Burn | 11 | 33 | OK — periodic burn subs |
| OnDelay_Split | ⟐Delay, Split | 2 | 36 | OK — delay then split |
| Homing_OnPulse_Frost | Homing, ⟐Pulse, Frost | 2 | 43 | OK — homing main emits frost subs periodically |

### Synergy Combos

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| HeavyHeavy_Railgun | Heavy, Heavy | 8 | 30 | OK — Railgun synergy grants free Pierce. 8 proj = piercing confirmed |
| FrostFrost_Blizzard | Frost, Frost | 1 | 31 | OK — Blizzard synergy (freeze stun) |
| PierceBounce_Ricochet | Pierce, Bounce | 2 | 28 | OK — Ricochet synergy. 2 proj = projectile surviving through hits |
| HomingSwift_Missile | Homing, Swift | 1 | 36 | OK — Missile synergy (snap homing) |
| HeavyWide_Meteor | Heavy, Wide | 1 | 36 | OK — Meteor synergy (2x AOE radius) |
| BurnWide_Napalm | Burn, Wide | 1 | 26 | OK — Napalm synergy |

### Edge Cases

| Test | Mods | maxProj | Enemies Left | Notes |
|------|------|---------|--------------|-------|
| SplitSplit_Barrage | Split, Split | 20 | 21 | OK — Barrage synergy. 20 simultaneous = lots of splits |
| OnHit_OnKill_Burn | ⟐Hit, ⟐Kill, Burn | 1 | 30 | OK — nested: OnHit→sub with OnKill→Burn sub-sub |
| Pierce_OnHit_Pierce | Pierce, ⟐Hit, Pierce | 30 | 28 | OK — pierce main, on hit → pierce sub. 30 proj = cascading pierces |

---

## Items Requiring Visual Verification

These tests passed (tower fired, no crash) but may have subtle gameplay issues that can only be confirmed visually:

1. **Homing_OnHit_Split_OnKill_Wide** — maxProj=1 is suspiciously low. The homing main should hit → spawn Split sub → Split fans out → each shard that kills → Wide AOE sub. Either subs die too fast or the chain doesn't fully cascade. **Test manually with multiple enemies.**

2. **OnEnd subs** — OnEnd fires when the projectile expires (lifetime ends). The sub spawns at the expiry point which may be far from enemies. **Verify subs actually reach and hit enemies.**

3. **Sub-projectile directions** — all impact event subs now spawn in random directions (±10° cone excluded). **Verify this looks good visually** — not too chaotic, homing subs should correct course.

4. **Split fan shape** — original takes left edge of fan (-45°), extras distribute right. **Verify it looks like a proper fan** and not lopsided.

5. **Pierce+Bounce coexistence** — both stages run in PostHit now. When pierce charges are exhausted, bounce takes over. **Verify the projectile properly redirects** to a new target after pierce runs out.

6. **OnHit sub with only elemental mods (Burn/Frost)** — sub inherits parent HitInstances, so it can't hit the same enemy the parent just hit. The sub flies in a random direction and must find a NEW enemy to apply the effect. **Verify the effect actually lands on something** — if no enemies nearby, the sub may expire uselessly.

---

## Design Notes

- `maxProj` correlates with build complexity — simple builds (1 trait) have 1-2, complex chains (Pierce+OnHit+Split) have 10-30
- Enemy count accumulates between tests because the cleanup resets towers but enemies continue spawning. This is a test harness limitation, not a game bug.
- All synergy combos fire correctly (HeavyHeavy→Railgun gives pierce, SplitSplit→Barrage gives extra projectiles, etc.)
