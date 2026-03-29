# Sound Design — Gridlock

Tous les sons en format **WAV** (PCM 16-bit, mono). Ce document liste chaque son necessaire dans le jeu.

## Legende

- ✅ **Fait** — son final, integre dans le jeu
- 🔄 **Placeholder** — un son temporaire existe (Kenney.nl CC0), a remplacer
- ❌ **Manquant** — aucun son, a creer

---

## Tours — Tir

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 1 | Tir de tour (x4) | `tower_fire_00.wav` ... `_03.wav` | 🔄 | Quand une tour tire un projectile. |
| 2 | Impact projectile (x4) | `projectile_impact_00.wav` ... `_03.wav` | 🔄 | Quand un projectile touche un ennemi. |

## Tours — Placement

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 3 | Placement de tour (x3) | `tower_place_01.wav` ... `_03.wav` | 🔄 | Quand le joueur pose une tour sur la grille. |

## Ennemis

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 4 | Ennemi touche (x4) | `enemy_hit_00.wav` ... `_03.wav` | 🔄 | Quand un ennemi recoit des degats. |
| 5 | Ennemi mort (x4) | `enemy_death_00.wav` ... `_03.wav` | 🔄 | Quand un ennemi meurt et explose en voxels. |

## Objectif

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 6 | Objectif touche (x2) | `objective_hit_00.wav` ... `_01.wav` | 🔄 | Quand un ennemi atteint l'objectif et lui inflige des degats. |

## Vagues

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 7 | Debut de vague (x2) | `wave_start_00.wav` ... `_01.wav` | 🔄 | Quand le joueur lance une vague d'ennemis. |
| 8 | Vague terminee (x2) | `wave_complete_00.wav` ... `_01.wav` | 🔄 | Quand tous les ennemis d'une vague sont elimines. |

## Mod Slot Editor

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 9 | Placer un mod (x2) | `mod_place_00.wav` ... `_01.wav` | ❌ | Quand le joueur drop un mod dans un slot. |
| 10 | Retirer un mod (x2) | `mod_remove_00.wav` ... `_01.wav` | ❌ | Quand le joueur retire un mod d'un slot. |
| 11 | Swap de mods (x2) | `mod_swap_00.wav` ... `_01.wav` | ❌ | Quand le joueur echange deux mods de position. |
| 12 | Synergie declenchee (x2) | `mod_synergy_00.wav` ... `_01.wav` | ❌ | Quand deux mods adjacents creent une synergie. |
| 13 | Ouverture panel tour (x2) | `editor_open_00.wav` ... `_01.wav` | 🔄 | Quand le joueur clique sur une tour pour l'editer. |
| 14 | Fermeture panel tour (x2) | `editor_close_00.wav` ... `_01.wav` | 🔄 | Quand le joueur ferme le panel de la tour. |

## Effets elementaires (projectiles)

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 15 | Burn proc (x2) | `elem_burn_00.wav` ... `_01.wav` | ❌ | DOT feu applique. Petit crackle. |
| 16 | Frost proc (x2) | `elem_frost_00.wav` ... `_01.wav` | ❌ | Slow applique. Cristallisation. |
| 17 | Shock proc (x2) | `elem_shock_00.wav` ... `_01.wav` | ❌ | Chaine electrique. Zap court. |
| 18 | Void proc (x2) | `elem_void_00.wav` ... `_01.wav` | ❌ | % HP drain. Son grave, sourd. |
| 19 | Split proc (x2) | `elem_split_00.wav` ... `_01.wav` | ❌ | Projectile se divise. Pop eclatant. |
| 20 | Explosion AOE (x3) | `explosion_aoe_00.wav` ... `_02.wav` | ❌ | Wide impact. Boom etoffe. |

## Events

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 21 | Event trigger (x2) | `event_trigger_00.wav` ... `_01.wav` | ❌ | Quand un event ⟐ se declenche (son subtil de transition). |
| 22 | Synergy combo (x2) | `synergy_combo_00.wav` ... `_01.wav` | ❌ | Quand un conditionnel inter-tours se declenche (If-Burning etc). Satisfaisant. |

## UI Generale

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 23 | Clic bouton (x3) | `ui_click_01.wav` ... `_03.wav` | 🔄 | Quand le joueur clique sur un bouton UI. |

---

## Resume

| Statut | Nombre |
|--------|--------|
| 🔄 Placeholder | 10 |
| ❌ Manquant | 13 |
| **Total** | **23** |

---

## Convention de nommage

Tous les fichiers en **WAV** (PCM 16-bit, mono).

```
<categorie>_<detail>_<variante>.wav
```

Variantes numerotees sur 2 chiffres, commencant a `00`.

### Exemples

```
tower_fire_00.wav
mod_place_01.wav
elem_burn_00.wav
event_trigger_01.wav
synergy_combo_00.wav
```
