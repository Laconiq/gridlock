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

## UI — General

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 23 | Clic bouton (x3) | `ui_click_01.wav` ... `_03.wav` | 🔄 | Quand le joueur clique sur un bouton UI. |
| 24 | Hover bouton/slot (x2) | `ui_hover_00.wav` ... `_01.wav` | 🔄 | Survol d'un element interactif. Son subtil, tick leger. |
| 25 | Annonce HUD (x1) | `ui_announce_00.wav` | 🔄 | Annonce texte a l'ecran (wave cleared, etc.). Whoosh court. |

## UI — Drag & Drop (Mod Slots)

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 26 | Drag start (x2) | `ui_drag_start_00.wav` ... `_01.wav` | 🔄 | Quand on commence a drag un mod depuis un slot ou l'inventaire. |
| 27 | Drag hover valid (x1) | `ui_drag_hover_00.wav` | 🔄 | Survol d'un slot valide pendant le drag. Tick doux. |
| 28 | Drop fail (x2) | `ui_drop_fail_00.wav` ... `_01.wav` | 🔄 | Drop hors zone — mod retourne a l'origine. Buzz/rejection. |

## UI — Dropdown

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 29 | Dropdown open (x1) | `ui_dropdown_open_00.wav` | 🔄 | Ouverture du selecteur de ciblage. Pop leger. |
| 30 | Dropdown select (x1) | `ui_dropdown_select_00.wav` | 🔄 | Changement du mode de ciblage. Clic confirme. |

## Tours — Interactions

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 31 | Placement invalide (x2) | `tower_place_invalid_00.wav` ... `_01.wav` | 🔄 | Tentative de placement sur case invalide. Buzz court. |
| 32 | Survol tour (x1) | `tower_hover_00.wav` | 🔄 | Survol d'une tour sur la grille. Hum electronique subtil. |

## Loot / Pickup

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 33 | Loot drop (x2) | `loot_drop_00.wav` ... `_01.wav` | 🔄 | Module pickup spawn a la mort d'un ennemi. Tinkle metallique. |
| 34 | Loot collect (x2) | `loot_collect_00.wav` ... `_01.wav` | 🔄 | Pickup aspire vers l'inventaire (magnet). Whoosh ascendant. |

## Game State

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 35 | Game over (x1) | `game_over_00.wav` | 🔄 | Objectif detruit, defaite. Impact grave + decay. |

---

## Audio Mixer

```
Master
├── SFX      — tours, ennemis, projectiles, elements, loot, grid warp
│   └── Lowpass (active quand l'editeur de tour est ouvert)
└── UI       — clics, drag, hover, dropdown, editor open/close, annonces
```

Le lowpass sur SFX attenue le monde du jeu quand le joueur est concentre sur l'edition de sa tour. Cutoff ~800 Hz a l'ouverture, retour a 22 kHz a la fermeture, avec interpolation lissee.

---

## Resume

| Statut | Nombre |
|--------|--------|
| 🔄 Placeholder | 22 |
| ❌ Manquant | 13 |
| **Total** | **35** |

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
