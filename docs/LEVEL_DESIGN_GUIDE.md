# Gridlock — Level Design Guide

Guide de conception de niveaux pour un Tower Defense isométrique sur grille.

## Table des matières

1. [Outils et pipeline](#outils-et-pipeline)
2. [La grille](#la-grille)
3. [Paths](#paths)
4. [Placement stratégique](#placement-stratégique)
5. [Pacing et difficulté](#pacing-et-difficulté)
6. [Checklist de validation](#checklist-de-validation)

---

## Outils et pipeline

### GridDefinition ScriptableObject

Les niveaux sont des `GridDefinition` ScriptableObjects éditables dans l'Inspector (Odin).

- **Fichiers** : `Assets/Data/Levels/`
- **Création** : menu `Gridlock/Create Test Grid Level` ou clic droit → Create → Gridlock → Grid Definition

### Structure

| Champ | Description |
|-------|-------------|
| `width` | Largeur de la grille en cellules |
| `height` | Hauteur de la grille en cellules |
| `cellSize` | Taille d'une cellule en unités monde |
| `cells` | Array plat de `CellType` (indexé `y * width + x`) |
| `paths` | Liste de `PathDefinition` (routeId + waypoints) |
| `objectiveHP` | Points de vie de l'objectif |

### Cell Types

| Type | Usage |
|------|-------|
| `Empty` | Case vide, le joueur peut y poser une tour |
| `Path` | Chemin des ennemis (non constructible) |
| `TowerSlot` | Emplacement suggéré pour une tour (constructible) |
| `Blocked` | Case occupée par une tour posée (set at runtime, never in the SO) |
| `Spawn` | Point d'apparition des ennemis |
| `Objective` | L'objectif à défendre |

### Workflow

1. Créer un `GridDefinition` SO
2. Définir width, height, cellSize
3. Cliquer "Initialize Grid" pour créer le tableau de cellules
4. Peindre les cellules dans l'Inspector (ou via un editor tool futur)
5. Définir les paths : liste de waypoints `Vector2Int` dans l'ordre de parcours
6. Placer Spawn et Objective
7. Tester en Play Mode

---

## La grille

### Dimensions recommandées

| Taille | Dimensions | Monde (cellSize=2) | Usage |
|--------|-----------|---------------------|-------|
| Petite | 16x10 | 32x20 unités | Tutoriel |
| Standard | 24x14 | 48x28 unités | Niveau normal |
| Grande | 32x18 | 64x36 unités | Niveaux avancés |

### Principes

- **Tout est sur la grille** : ennemis, tours, effets visuels
- **Le path doit être visible** : le `PathVisualizer` dessine les routes en LineRenderer
- **Espace libre autour du path** : laisser des cases `Empty` pour le placement de tours
- **Gameplay en XZ** : Y est réservé à la hauteur visuelle (ennemis flottent à Y=0.5)
- **Grid warp** : la grille se déforme à chaque impact/kill (mass-spring physics style Geometry Wars). Tous les objets (tours, ennemis, projectiles, path) suivent la surface déformée via `WarpFollower`
- **Runtime cells** : `GridManager` clone les cellules du SO au démarrage. Les modifications runtime (placement de tours → `Blocked`) ne corrompent jamais le SO

---

## Paths

### Conception des chemins

Les ennemis suivent les waypoints **segment par segment** — ils ne coupent jamais en diagonale. Le path doit être pensé comme une ligne sur la grille.

### Formes de path

| Forme | Description | Intérêt |
|-------|-------------|---------|
| **Ligne droite** | Horizontal ou vertical | Simple, pour tutoriel |
| **S-curve** | Zigzag horizontal avec virages | Standard, bon dwell time |
| **Spiral** | Enroulement vers le centre | High dwell time, tours centrales premium |
| **Dual-lane** | Deux paths convergents | Force la répartition des 5 tours |
| **Cross** | Paths qui se croisent | Positions d'intersection à haute valeur |

### Waypoints

- Chaque `PathDefinition` est une liste ordonnée de `Vector2Int`
- Les waypoints sont des coordonnées de cellules sur la grille
- Le dernier waypoint avant l'objectif est automatiquement ajouté par `GridManager`
- Les ennemis spawn sur la cellule `Spawn` et avancent vers le premier waypoint

### Dwell time

Le *dwell time* est le temps qu'un ennemi passe exposé aux défenses. Plus le path est long, plus les tours ont le temps de tirer.

| Niveau | Path length | Dwell time (~speed 2) |
|--------|-------------|----------------------|
| Facile | 30+ cells | ~30s |
| Normal | 20-30 cells | ~15-20s |
| Difficile | 15-20 cells | ~10-15s |

---

## Placement stratégique

### Positions de valeur

Le level design doit creer des **positions de valeur inegale** :

- **Position premium** : couvre 2+ segments du path (virage, intersection, switchback)
- **Position standard** : couvre 1 segment
- **Position piege** : semble bonne mais ne couvre qu'un angle

### Creer des choix

Le joueur doit se demander :
- Couvrir l'entree ou la sortie ?
- Concentrer les tours ou les disperser ?
- Defense en profondeur (tours espacees) ou killzone (tours groupees) ?
- Ou placer les tours de setup (Frost/Burn) vs les tours de payoff (If-Frozen/If-Burning) ?

### Synergies inter-tours et placement

Le systeme de Mod Slots encourage les **combos inter-tours** via les events conditionnels (⟐ If-Burning, ⟐ If-Frozen, etc.). Le level design doit creer des zones ou :
- Plusieurs tours ont des portees qui se chevauchent (pour que les conditionnels fonctionnent)
- Les tours de setup (debut du path) preparent les ennemis pour les tours de payoff (fin du path)
- Les intersections de paths sont des positions premium pour les tours AOE (Wide) et les conditionnels

### Tips

- **Switchbacks** : quand le path repasse devant la meme zone, une tour bien placee couvre les deux passages
- **Virages** : les positions a l'interieur du virage ont un meilleur coverage
- **Intersections** : si deux paths se croisent, la position centrale couvre les deux
- Les cases `TowerSlot` peuvent guider le joueur vers les bonnes positions (optionnel)
- **Killzones** : regrouper 3+ tours sur un segment pour maximiser les synergies conditionnelles

---

## Pacing et difficulté

### Courbe des vagues

```
Intensité
│
│              ╱──╲
│           ╱╱     ╲╲   ← Climax
│        ╱╱
│     ╱╱
│  ╱╱
│╱
└──────────────────── Waves
  1   2   3   4   5
```

- **Wave 1** : 5-10 ennemis lents → apprendre le flow
- **Waves 2-3** : plus d'ennemis, vitesse croissante
- **Waves 4-5** : ennemis variés, pression maximale

### Wave Definition

Chaque wave est un `WaveDefinition` SO avec des `WaveEntry` :
- `enemy` : référence à un `EnemyDefinition`
- `count` : nombre d'ennemis
- `spawnInterval` : temps entre chaque spawn
- `delayBeforeGroup` : délai avant ce groupe

---

## Checklist de validation

### Avant de finaliser un niveau :

- [ ] **Spawn** : au moins 1 cellule `Spawn` sur le bord de la grille
- [ ] **Objective** : 1 cellule `Objective`, idéalement côté opposé au spawn
- [ ] **Path continu** : les waypoints forment un chemin continu de cellule en cellule
- [ ] **Path marqué** : toutes les cellules du chemin sont en type `Path`
- [ ] **Espace libre** : assez de cases `Empty` autour du path pour 5 tours
- [ ] **Dwell time** : le path est assez long pour que les tours aient le temps de tirer
- [ ] **Choix** : il y a des positions de valeur inégale (pas toutes équivalentes)
- [ ] **ObjectiveHP** : défini dans le GridDefinition
- [ ] **Test en Play Mode** : les ennemis suivent bien le path sans diagonales
- [ ] **Caméra** : la grille entière est visible en iso avec zoom par défaut
