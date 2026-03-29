# Sound Design — Gridlock

Tous les sons en format **WAV** (PCM 16-bit, mono). Ce document liste chaque son nécessaire dans le jeu, ce qui est fait et ce qui reste à faire.

## Légende

- ✅ **Fait** — son final, intégré dans le jeu
- 🔄 **Placeholder** — un son temporaire existe (Kenney.nl CC0), à remplacer
- ❌ **Manquant** — aucun son, à créer

---

## Tours — Tir

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 1 | Tir de tour (×4) | `tower_fire_00.wav` … `_03.wav` | 🔄 | Quand une tour tire un projectile. |
| 2 | Impact projectile (×4) | `projectile_impact_00.wav` … `_03.wav` | 🔄 | Quand un projectile touche un ennemi. |

## Tours — Placement

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 3 | Placement de tour (×3) | `tower_place_01.wav` … `_03.wav` | 🔄 | Quand le joueur pose une tour sur la grille. |

## Ennemis

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 4 | Ennemi touché (×4) | `enemy_hit_00.wav` … `_03.wav` | 🔄 | Quand un ennemi reçoit des dégâts. |
| 5 | Ennemi mort (×4) | `enemy_death_00.wav` … `_03.wav` | 🔄 | Quand un ennemi meurt et explose en voxels. |

## Objectif

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 6 | Objectif touché (×2) | `objective_hit_00.wav` … `_01.wav` | 🔄 | Quand un ennemi atteint l'objectif et lui inflige des dégâts. |

## Vagues

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 7 | Début de vague (×2) | `wave_start_00.wav` … `_01.wav` | 🔄 | Quand le joueur lance une vague d'ennemis. |
| 8 | Vague terminée (×2) | `wave_complete_00.wav` … `_01.wav` | 🔄 | Quand tous les ennemis d'une vague sont éliminés. |

## Éditeur Nodal — Nodes

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 9 | Prendre un node (×2) | `node_grab_00.wav` … `_01.wav` | 🔄 | Quand le joueur attrape un node pour le déplacer. |
| 10 | Poser un node (×2) | `node_drop_00.wav` … `_01.wav` | 🔄 | Quand le joueur relâche un node sur le canvas. |
| 11 | Supprimer un node (×2) | `node_remove_00.wav` … `_01.wav` | 🔄 | Quand le joueur drag un node vers la sidebar pour le supprimer. |

## Éditeur Nodal — Connexions

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 12 | Connecter deux ports (×2) | `port_connect_00.wav` … `_01.wav` | 🔄 | Quand le joueur relie un output à un input. |
| 13 | Déconnecter (×2) | `port_disconnect_00.wav` … `_01.wav` | 🔄 | Quand une connexion existante est remplacée par une nouvelle. |

## Éditeur Nodal — Ouverture / Fermeture

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 14 | Ouverture éditeur (×2) | `editor_open_00.wav` … `_01.wav` | 🔄 | Quand le panneau de l'éditeur nodal s'ouvre. |
| 15 | Fermeture éditeur (×2) | `editor_close_00.wav` … `_01.wav` | 🔄 | Quand le panneau de l'éditeur nodal se ferme. |

## UI Générale

| # | Son | Fichier | Statut | Description |
|---|-----|---------|--------|-------------|
| 16 | Clic bouton (×3) | `ui_click_01.wav` … `_03.wav` | 🔄 | Quand le joueur clique sur un bouton UI. |

---

## Résumé

| Statut | Nombre |
|--------|--------|
| 🔄 Placeholder | 16 |
| **Total** | **16** |

---

## Convention de nommage

Tous les fichiers en **WAV** (PCM 16-bit, mono).

```
<catégorie>_<détail>_<variante>.wav
```

Variantes numérotées sur 2 chiffres, commençant à `00`.

### Exemples

```
tower_fire_00.wav
tower_fire_03.wav
enemy_death_02.wav
port_connect_00.wav
editor_open_01.wav
ui_click_01.wav
```

