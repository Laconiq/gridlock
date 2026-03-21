# Modules Roadmap

## Legend
- ✅ **Done** — Implemented, SO created, in ModuleRegistry
- 🔧 **WIP** — Code exists but needs polish/fixes
- 📋 **Planned** — Designed, not yet implemented
- 💡 **Idea** — Brainstorm, needs design

---

## TRIGGERS (When)

| Module | Status | ID | Description |
|--------|--------|----|-------------|
| On Timer | ✅ | `on_timer` | Fires at regular interval (configurable seconds) |
| On Left Click | ✅ | `trigger_onleftclick` | Fires on left mouse button (player weapon, fixed node) |
| On Right Click | ✅ | `trigger_onrightclick` | Fires on right mouse button (player weapon, fixed node) |
| On Enemy Enter Range | ✅ | `on_enemy_enter_range` | Fires when an enemy enters the chassis range |
| On Kill | 📋 | — | Fires when the chassis kills an enemy |
| On Player Damaged | 📋 | — | Fires when the owning player takes damage |
| On Wave Start | 📋 | — | Fires once when a new wave begins |
| On Reload | 💡 | — | Fires when player reloads (if ammo system added) |
| On Ally Nearby | 💡 | — | Fires when another player is within range |

---

## TARGETS (Where / Who)

| Module | Status | ID | Description |
|--------|--------|----|-------------|
| Nearest Enemy | ✅ | `nearest_enemy` | Selects the closest enemy in range (OverlapSphere) |
| All Enemies In Range | ✅ | `all_enemies_in_range` | Selects all enemies in range (OverlapSphere) |
| Forward Aim | ✅ | `forward_aim` | Creates aim target along FirePoint.forward (FPS aiming) |
| Cone Target | 📋 | — | Selects enemies in a forward cone (angle + range) |
| Line Target | 📋 | — | Selects enemies along a line/ray (piercing) |
| Self Target | 📋 | — | Selects the owning player (for self-buffs) |
| Allies In Range | 📋 | — | Selects allied players in range |
| Lowest HP Enemy | 💡 | — | Selects the enemy with lowest HP% |
| Highest HP Enemy | 💡 | — | Selects the enemy with highest HP% |
| Random Enemy | 💡 | — | Selects a random enemy in range |
| AoE Around Target | 💡 | — | Takes targets from previous target, expands selection around each |

---

## EFFECTS (What)

| Module | Status | ID | Description |
|--------|--------|----|-------------|
| Projectile | ✅ | `projectile` | Spawns a projectile that flies and deals damage on hit |
| Hitscan | ✅ | `hitscan` | Instant damage to targets (no travel time) |
| Damage Over Time | ✅ | `dot` | Applies periodic damage over duration |
| Slow | ✅ | `slow` | Reduces target move speed for duration |
| Heal | 📋 | — | Restores HP to targets (players/allies) |
| Shield | 📋 | — | Grants temporary damage absorption |
| Knockback | 📋 | — | Pushes targets away from origin |
| Chain Lightning | 📋 | — | Damage bounces between nearby enemies |
| Freeze | 📋 | — | Immobilizes target for duration (0 speed) |
| Burn (AoE DoT) | 📋 | — | Creates a ground area that damages enemies over time |
| Speed Boost | 💡 | — | Increases ally move speed |
| Teleport | 💡 | — | Instantly moves target to a position |
| Mark Target | 💡 | — | Marks enemy to take increased damage |
| Explosion | 💡 | — | AoE damage at target position |
| Lifesteal | 💡 | — | Damages target and heals the player |
| Stun | 💡 | — | Stops enemy movement and attacks briefly |
| Gravity Well | 💡 | — | Pulls enemies toward a point |
| Ricochet | 💡 | — | Projectile bounces to nearby enemies on hit |

---

## Module Combos (design ideas)

These illustrate how chaining creates emergent gameplay:

```
On Timer → All In Range → Slow        → DOT          (crowd control tower)
On Timer → Nearest Enemy → Projectile → Explosion    (single target + splash)
On Left Click → Forward Aim → Hitscan → Chain Lightning (FPS chain damage)
On Enemy Enter → Cone → Knockback               (area denial)
On Kill → Self → Shield                          (reward on kill)
On Player Damaged → Self → Heal                  (reactive defense)
On Timer → Lowest HP → Mark Target → Hitscan          (focus fire)
```

---

## Implementation Priority

### Phase 1 — Core (done)
Basic trigger/target/effect for both player weapon and tower.

### Phase 2 — Variety
Cone, Heal, Knockback, Freeze — enough diversity for interesting builds.

### Phase 3 — Advanced
Chain Lightning, Explosion, Mark Target — combo enablers.

### Phase 4 — Creative
Gravity Well, Teleport, Ricochet — unique mechanics.
