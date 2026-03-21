# Modules Roadmap

## Legend
- ✅ **Done** — Implemented, SO created, in ModuleRegistry
- 📋 **Planned** — Designed, not yet implemented

---

## Design Philosophy

Every module works **identically** on player weapons and tower turrets. No module is "player-only" or "tower-only" (except the fixed click triggers on the player weapon). The `Trigger → Target → Effect` chain behaves the same regardless of what chassis it's on.

Rarity = specialization, not power:
- **Common**: reliable, no conditions
- **Uncommon**: one interesting mechanic or tradeoff
- **Rare**: enables combos, build-defining
- **Epic**: transforms playstyle, requires co-op coordination

---

## TRIGGERS (When does this fire?)

All triggers work the same on player and tower. The chassis provides the context (FirePoint, range), the trigger just decides WHEN.

| Module | Rarity | Status | ID | Description |
|--------|--------|--------|----|-------------|
| On Left Click | — | ✅ | `trigger_onleftclick` | Fires on left click. Fixed node on player weapon. |
| On Right Click | — | ✅ | `trigger_onrightclick` | Fires on right click. Fixed node on player weapon. |
| On Timer | Common | ✅ | `on_timer` | Fires at regular interval (configurable seconds) |
| On Enemy Enter Range | Common | ✅ | `on_enemy_enter_range` | Fires when an enemy enters the chassis range |
| On Enemy Exit Range | Common | ✅ | `on_enemy_exit_range` | Fires when an enemy leaves the chassis range |
| On Kill | Uncommon | 📋 | — | Fires when the chain kills an enemy (reward chaining) |
| On Hit | Uncommon | 📋 | — | Fires on successful hit (proc chance configurable) |
| On Ally Hit | Uncommon | 📋 | — | Fires when a nearby ally takes damage (defensive reaction) |
| On Burst | Rare | 📋 | — | Fires N shots rapidly, then pauses (burst-fire pattern) |
| On Wave Start | Rare | 📋 | — | Fires once when a new wave begins (opening salvo) |
| On Health Threshold | Rare | 📋 | — | Fires when chassis owner drops below X% HP (panic mode) |
| On Charge | Epic | 📋 | — | Accumulates power over hold time, fires on release (charged shot) |

---

## TARGETS (Who / where does it hit?)

All targets work the same on player and tower. They select from enemies/allies in the chassis range using the chassis FirePoint as reference direction.

Targets can be **chained serially** (Target → Target) to filter or transform selections.

| Module | Rarity | Status | ID | Description |
|--------|--------|--------|----|-------------|
| Forward Aim | Common | ✅ | `forward_aim` | Creates aim target along FirePoint.forward (directional fire) |
| Nearest Enemy | Common | ✅ | `nearest_enemy` | Selects the closest enemy in range |
| All Enemies In Range | Common | ✅ | `all_enemies_in_range` | Selects all enemies in range |
| Strongest Enemy | Common | ✅ | `strongest_enemy` | Selects enemy with the highest current HP |
| Weakest Enemy | Common | ✅ | `weakest_enemy` | Selects enemy with the lowest current HP |
| First In Path | Common | ✅ | `first_in_path` | Selects enemy closest to the objective |
| Last In Path | Common | ✅ | `last_in_path` | Selects enemy furthest from the objective |
| Random Enemy | Common | ✅ | `random_enemy` | Selects a random enemy in range |
| Cone | Uncommon | 📋 | — | Selects all enemies in a forward cone (angle configurable) |
| Line | Uncommon | 📋 | — | Selects enemies along a line from FirePoint (pierce) |
| Lowest Health % | Uncommon | ✅ | `lowest_health_percent` | Selects enemy with lowest HP percentage |
| Highest Health % | Uncommon | ✅ | `highest_health_percent` | Selects enemy with highest HP percentage |
| Chain Target | Rare | 📋 | — | From primary target, chains to nearby enemies (configurable count + range) |
| Splash Radius | Rare | 📋 | — | Expands around each target position with configurable radius |
| Ground Point | Rare | 📋 | — | Creates target at FirePoint ground intersection (mortar/lob) |
| Allies In Range | Rare | ✅ | `allies_in_range` | Selects allied players/towers in range (for support effects) |
| Self Target | Rare | ✅ | `self_target` | Selects the chassis owner itself (self-buff) |
| Tagged Target | Epic | 📋 | — | Selects only enemies marked with a tag (co-op synergy) |

### Target Chaining Examples
```
AllEnemiesInRange → LowestHealth%     = AoE scan, pick the most wounded
Cone → StrongestEnemy                 = Cone spread, only hits the toughest
NearestEnemy → ChainTarget            = Hit nearest, then chain to nearby
ForwardAim → SplashRadius             = Aim at a point, splash around impact
```

---

## EFFECTS (What does it do?)

All effects work the same on player and tower. They receive the target list from the target module and execute on each target.

Effects can be **chained vertically** (Effect → Effect) on the same targets.

### Direct Damage

| Module | Rarity | Status | ID | Description |
|--------|--------|--------|----|-------------|
| Projectile | Common | ✅ | `projectile` | Spawns a projectile toward target, deals damage on hit |
| Hitscan | Common | ✅ | `hitscan` | Instant damage to target (no travel time) |
| Explosion | Uncommon | 📋 | — | AoE damage at target position (configurable radius + falloff) |
| Pierce Projectile | Uncommon | 📋 | — | Projectile passes through N enemies, damaging each |
| Bounce Projectile | Rare | 📋 | — | Projectile ricochets between enemies (configurable count + decay) |
| Beam | Rare | 📋 | — | Continuous damage beam to target (DPS over duration) |
| Execute | Epic | 📋 | — | Bonus damage multiplier when target is below HP threshold |

### Status / Crowd Control

| Module | Rarity | Status | ID | Description |
|--------|--------|--------|----|-------------|
| Slow | Common | ✅ | `slow` | Reduces target move speed for duration |
| Damage Over Time | Common | ✅ | `dot` | Periodic damage ticks over duration |
| Stun | Uncommon | 📋 | — | Stops enemy movement and attacks for duration |
| Knockback | Uncommon | ✅ | `knockback` | Pushes target away from origin with configurable force |
| Weaken | Uncommon | ✅ | `weaken` | Reduces target damage output for duration |
| Vulnerability | Rare | ✅ | `vulnerability` | Target takes increased damage from all sources for duration |
| Root | Rare | 📋 | — | Immobilizes target but allows attacks |
| Freeze | Epic | 📋 | — | Completely freezes target. Killed while frozen = AoE shatter |
| Confuse | Epic | 📋 | — | Target attacks other enemies for duration |

### Utility / Support

| Module | Rarity | Status | ID | Description |
|--------|--------|--------|----|-------------|
| Heal | Uncommon | ✅ | `heal` | Restores HP to allies in target selection |
| Shield | Rare | 📋 | — | Grants temporary damage absorption to allies |
| Speed Boost | Rare | ✅ | `speed_boost` | Increases ally movement speed for duration |
| Damage Boost | Rare | ✅ | `damage_boost` | Increases ally damage output for duration |
| Pull | Rare | 📋 | — | Pulls enemies toward target point with configurable force |
| Teleport | Epic | 📋 | — | Moves enemy to a new position, resets path progress |

---

## Build Examples

All builds work on either player weapon or tower — the chassis is interchangeable.

**"Shotgun"** (Common)
```
Trigger → Cone → Projectile
```
Fires projectiles at all enemies in a forward cone.

**"Sniper"** (Rare)
```
OnCharge → ForwardAim → Hitscan → Vulnerability
```
Charged shot, instant hit, marks target vulnerable for the team.

**"Chain Gun"** (Rare)
```
Trigger → NearestEnemy → ChainTarget → Projectile
```
Hit nearest, chain projectile to 3 nearby enemies.

**"Gravity Bomb"** (Epic)
```
Trigger → AllEnemiesInRange → Pull
Trigger → AllEnemiesInRange → Explosion
```
Pull enemies to center, then explode them all.

**"Combat Medic"** (Rare)
```
Trigger → AlliesInRange → Heal → SpeedBoost
```
Heals and speeds up nearby allies.

**"Sniper Nest"** (Rare)
```
OnTimer → StrongestEnemy → Hitscan → Vulnerability
```
Periodically marks the toughest enemy vulnerable for the team.

**"Crowd Control"** (Epic)
```
OnEnemyEnterRange → AllEnemiesInRange → Freeze
OnKill → SplashRadius → Explosion
```
Freezes enemies in range. Killing a frozen one shatters it with AoE.

### Co-op Synergy

**"Mark & Execute"**
- Chassis A: `Trigger → ForwardAim → TaggedTarget` (marks enemies)
- Chassis B: `OnTimer → TaggedTarget → Hitscan → Execute` (kills marked)

**"Slow Corridor"**
- Chassis 1: `OnEnemyEnterRange → AllEnemiesInRange → Slow`
- Chassis 2: `OnEnemyEnterRange → AllEnemiesInRange → Vulnerability`
- All chassis: Focus fire the slowed, vulnerable enemies

---

## Implementation Priority

### Phase 1 — Core Variety
Triggers: `OnKill`, `OnBurst`
Targets: `StrongestEnemy`, `WeakestEnemy`, `Cone`, `Line`
Effects: `Explosion`, `Stun`, `Knockback`

### Phase 2 — Advanced Targeting
Targets: `SplashRadius`, `ChainTarget`, `FirstInPath`, `LastInPath`
Effects: `PierceProjectile`, `BounceProjectile`, `Vulnerability`

### Phase 3 — Support / Utility
Targets: `AlliesInRange`, `SelfTarget`
Effects: `Heal`, `Shield`, `DamageBoost`, `SpeedBoost`
Triggers: `OnAllyHit`

### Phase 4 — Epic
Effects: `Freeze`, `Confuse`, `Teleport`, `Pull`, `Execute`, `Beam`
Triggers: `OnCharge`, `OnHealthThreshold`
Targets: `TaggedTarget`, `GroundPoint`
