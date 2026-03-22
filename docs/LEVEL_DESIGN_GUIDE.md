# AIWE — Level Design Guide

Guide de conception de niveaux pour un Tower Defense coopératif FPS (2-4 joueurs).

## Table des matières

1. [Outils et pipeline](#outils-et-pipeline)
2. [Fondamentaux du TD level design](#fondamentaux-du-td-level-design)
3. [Pathing](#pathing)
4. [Chokepoints et killzones](#chokepoints-et-killzones)
5. [Placement des emplacements défensifs](#placement-des-emplacements-défensifs)
6. [Verticalité et sightlines](#verticalité-et-sightlines)
7. [Level design pour le co-op FPS](#level-design-pour-le-co-op-fps)
8. [Pacing et courbe de difficulté](#pacing-et-courbe-de-difficulté)
9. [Checklist de validation](#checklist-de-validation)
10. [Référence IntGrid](#référence-intgrid)
11. [Ressources externes](#ressources-externes)

---

## Outils et pipeline

### LDtk

Les niveaux sont conçus dans **LDtk** (Level Designer Toolkit) et importés automatiquement via `LDtkToUnity`.

- **Fichier projet** : `Assets/LDtk/testLevel.ldtk`
- **Layers** : `EnemyRoute` (waypoints), `Entities` (spawns, emplacements, objectif), `Terrain` (IntGrid)
- **Import** : le postprocesseur (`LDtkLevelPostprocessor`) convertit l'IntGrid 2D en blockout 3D (cubes, rampes) et transpose les coordonnées (X,Y → X,Z) avec calcul de hauteur automatique basé sur le terrain

### Workflow

1. Ouvrir le `.ldtk` dans LDtk
2. Blockout du terrain (IntGrid), placement des entités
3. Sauvegarder → Unity auto-reimport
4. Playtester en Play Mode (le NavMesh se bake au runtime via `RuntimeNavMeshBaker`)

### Entités disponibles

| Entité | Champs | Rôle |
|--------|--------|------|
| **PlayerSpawn** | `player_index` (0-3) | Spawn point joueur |
| **EnemySpawner** | `spawn_type`, `wave_group` | Spawn point ennemis |
| **EnemyPath** | `route_id`, `order` | Waypoint de la nav path ennemie |
| **TowerSlot** | `tower_id`, `max_triggers` (1-5) | Emplacement défensif (chassis) |
| **Objective** | `health` | Zone à défendre (redimensionnable) |

---

## Fondamentaux du TD level design

### Chaque map est un puzzle spatial

La rejouabilité d'un niveau TD repose sur la **lecture de map** (*map reading*) : le joueur analyse la géométrie pour identifier les positions optimales. Chaque map doit proposer un puzzle spatial distinct via :
- La topologie des paths (nombre, forme, intersections)
- La distribution et la valeur inégale des emplacements défensifs
- La verticalité et les sightlines
- Le ratio espaces fermés / espaces ouverts

> *"There should be high variance of strategic value across placement spots — not all spots should be equally good."*
> — [Kingdom Rush Campaign Level Design Analysis](https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design)

### Favoriser les choix significatifs

Un bon niveau génère des **trade-offs** :
- **Deep vs. wide** : concentrer les défenses sur une killzone ou étaler la couverture ?
- **Quelle lane prioriser** : les ressources sont limitées, chaque investissement est un pari
- **Défenses statiques vs. joueur FPS** : certaines zones sont mieux couvertes par des tours, d'autres par l'intervention directe du joueur

### Anti-patterns à éviter

| Anti-pattern | Problème | Solution |
|--------------|----------|----------|
| Straightaway unique | Un seul placement optimal, pas de trade-off | Bends, switchbacks, multi-paths |
| Emplacements homogènes | Aucune hiérarchie de valeur stratégique | Varier la couverture et le `max_triggers` |
| Lock-and-key enemies | Test de ressources, pas de stratégie | Toujours 2+ contre-mesures viables |
| Anti-killzone design | Frustrant, empêche le power fantasy | Laisser les joueurs construire des combos |
| Pas d'overview | Perte de lisibilité spatiale | Minimap ou overhead view en phase de préparation |

---

## Pathing

### Single path vs. multi-lane

| Layout | Contexte | Intérêt |
|--------|----------|---------|
| **Single path** | Tutoriel, introduction | Focus sur l'apprentissage |
| **Dual-lane convergent** | Niveaux co-op standard | Force le split des joueurs, convergence = tension |
| **3+ lanes** | Niveaux avancés | Haute pression sur l'allocation de ressources |

**Règle AIWE (2-4 joueurs)** : le dual-lane convergent est le sweet spot. Chaque joueur peut hold une lane, puis se coordonner au merge point.

### Formes de path

- **Switchbacks** : le path repasse devant le même emplacement 2-3 fois. Un seul slot couvre plusieurs segments → spot à très haute valeur (*high-value position*).
- **Bends** : éviter les straightaways. Les virages créent des positions différenciées — piercing sur les lignes droites, AoE dans les bends.
- **Merge point** : le point de convergence de deux lanes. Moment de tension maximale, idéal pour l'AoE et les effets de crowd control.

### Longueur de path

- **Niveau d'introduction** : paths longs (60+ cells de parcours). Plus de *dwell time* (temps d'exposition aux défenses) = plus tolérant.
- **Niveaux avancés** : paths courts. Chaque emplacement doit être optimal.
- **Règle FPS** : le path doit être assez long pour qu'un joueur puisse physiquement se repositionner (*rotate*) d'un point de breach à un autre avant que les ennemis n'atteignent l'objectif.

---

## Chokepoints et killzones

### Anatomie d'un chokepoint

Un chokepoint est un **bottleneck** (rétrécissement du path) où :
- Plusieurs emplacements défensifs ont des *overlapping fields of fire*
- Les ennemis sont canalisés → AoE maximisé
- Le joueur FPS a un *clear angle* depuis une position surélevée ou protégée

### Distribution des chokepoints

- **Espacer le long du path** : pas de stacking au même endroit. Chaque chokepoint doit représenter une décision d'investissement distincte.
- **2-3 chokepoints primaires** par lane, plus le merge point.
- **En co-op** : assez d'espace entre les chokepoints pour qu'un joueur ne puisse pas hold deux à la fois → chaque joueur a une responsabilité claire.

### Créer un chokepoint dans LDtk

```
Avant (corridor 6 cells)          Après (bottleneck 3 cells)
WWWWWWWW                           WWWWWWWW
FFFFFFFF  → enemy path →           FFFWWFFF  → enemy path →
FFFFFFFF                           FFFWWFFF
WWWWWWWW                           WWWWWWWW
                                   ↑ cover pillars
```

Le bottleneck concentre le flux ennemi. Placer des TowerSlots avec overlapping coverage des deux côtés.

---

## Placement des emplacements défensifs

### Principes

1. **Hiérarchie de valeur** : certains slots couvrent 2+ segments (switchback), d'autres un seul. Les joueurs doivent *lire la map* pour identifier les high-value positions.
2. **Varier les types de chassis** : exploiter les 3 supports de pose — Sentinelle (sol), Murale (mur), Plafonnier (plafond). Diversifier les opportunités de placement.
3. **Clustering autour des bottlenecks** : les meilleurs emplacements sont près des chokepoints. Ajouter des slots auxiliaires pour la couverture secondaire ou les builds situationnelles.
4. **Accessibilité en combat** : les joueurs doivent pouvoir atteindre et modifier leurs défenses pendant une wave sans backtracking excessif.

### Densité d'emplacements par map

| Taille de map | TowerSlots | Note |
|---------------|------------|------|
| Petite (intro) | 6-8 | Focus apprentissage, peu de trade-offs |
| Standard | 10-14 | Bon ratio choix / couverture |
| Grande (endgame) | 14-18 | Beaucoup de trade-offs, pression de ressources |

### Valeur stratégique via max_triggers

Moduler `max_triggers` pour refléter la valeur du slot :
- **3 triggers** : *premium positions* (HighGround central, merge point, switchback coverage)
- **2 triggers** : *standard positions* (corridor, flanking angle)
- **1 trigger** : *situational positions* (Barricade d'urgence, couverture de niche)

---

## Verticalité et sightlines

### HighGround (IntGrid value 5)

Plateformes élevées de 2 unités. Avantages :
- **Extended coverage** : les tours en hauteur ont un champ de tir plus large
- **Melee-safe** : hors de portée des ennemis au corps-à-corps
- **Sightlines dégagées** : le joueur FPS a un *overwatch* clair sur les paths en contrebas

### Ramps (IntGrid value 3)

Connectent le ground level au HighGround. Règles :
- Toujours adjacentes au HighGround qu'elles desservent
- Le postprocesseur génère automatiquement les marches
- Minimum 1-2 cells de rampe

### Sightline design

- **Walls = intentional LoS blockers** : créer des *dead zones* que le joueur FPS doit couvrir activement
- **HighGround ouvert** : permettre le tir plongeant (*plunging fire*) sur les paths en contrebas
- **Cover pillars** : partial cover dans les open areas, pas des murs pleins — casser les sightlines sans les supprimer

### Pattern : Overwatch position

```
HHHHHH      ← HighGround + TowerSlot (Piédestal)
RRRRRR      ← Ramp d'accès
FFFFFF      ← Ground level
FFFFFF      ← Enemy path
```

La position surélevée offre un *plunging fire* sur le path. Un des layouts les plus satisfaisants — le joueur voit et contrôle le flux ennemi depuis sa position d'overwatch.

---

## Level design pour le co-op FPS

### Le défi du hybride

> *"If towers handle everything, the FPS combat feels pointless. If the player is too powerful, towers feel pointless."*
> — [Combining Tower Defense with a Shooter](https://www.gamedeveloper.com/design/combining-tower-defense-with-a-shooter---game-design-implications)

### Résoudre par la géographie

1. **Alterner corridors et open arenas** :
   - **Corridors** = *tower power fantasy* (killzones, combos de modules, chained effects)
   - **Open arenas** = *player skill expression* (pas de canalisation, le joueur doit se repositionner et viser)

2. **Lane separation** :
   - 2+ spawn points sur des flancs opposés → les joueurs doivent split
   - Un joueur seul ne peut pas hold les deux lanes
   - Encourage les callouts : "Heavy wave, lane A !"

3. **Rôles émergents par la géométrie** :
   - **Anchor** : hold le merge point, gère le cluster de défenses central
   - **Roamer** : mobile, couvre les gaps, récupère le loot, intervient en renfort
   - **Overwatch** : posté sur le HighGround, couvre avec son arme et ses tours longue portée

### Phase de préparation

- Pas de timer pendant l'intermission (les joueurs planifient et coordonnent)
- Les emplacements défensifs doivent être faciles à repérer et accessibles
- Des sightlines claires depuis les zones de build vers les paths ennemis — le joueur doit voir ce qu'il défend

### Callouts de map

Nommer les zones pour la communication vocale :
- "Lane A" / "Lane B" pour les corridors principaux
- "Overwatch" ou "Balcon" pour les HighGround
- "Merge" ou "Croisement" pour le point de convergence
- "Zigzag" pour les switchbacks

---

## Pacing et courbe de difficulté

### Courbe du premier niveau

```
Intensité
│
│              ╱──╲
│           ╱╱     ╲╲   ← Climax (wave finale)
│        ╱╱          ╲
│     ╱╱
│  ╱╱     ← Ramping progressif
│╱
└──────────────────── Waves
  1   2   3   4   5   6
```

- **Wave 1** : facile (*gimme wave*) — 3-5 ennemis faibles, single lane. Apprendre le flow.
- **Waves 2-3** : introduction de la deuxième lane. Montée progressive.
- **Waves 4-5** : dual-lane actif. Pression croissante, premiers ennemis spéciaux.
- **Wave finale** : *skin-of-your-teeth* — le joueur doit sentir qu'il a tenu de justesse.

### Profil en dents de scie (*sawtooth pacing*)

Quand un nouveau mécanisme apparaît (nouveau type d'ennemi, nouvelle lane active), **baisser l'intensité** temporairement. Laisser les joueurs s'adapter, puis remonter la pression. Chaque "dent" est plus haute que la précédente.

### Timing des waves

- Pour un FPS TD co-op, **~8-12 waves** par map (au-delà, l'action se dilue — postmortem Sanctum 2)
- Intermissions assez longues pour repositionner les défenses et ramasser le loot
- **Pas de wave overlap** au premier niveau. Réserver les waves superposées pour les niveaux avancés.

---

## Checklist de validation

### Avant de finaliser un niveau :

- [ ] **Connectivity** : tout le floor est connecté (pas de poches isolées)
- [ ] **Bounding** : les walls enclosent complètement la map (pas de leak)
- [ ] **Corridor width** : minimum 3 cells (confort FPS, strafing possible)
- [ ] **Spawn points** : au moins 2 enemy spawners, sur des flancs opposés
- [ ] **Player spawns** : 4, avec `player_index` 0-3, près de l'objectif
- [ ] **Nav paths** : chaque spawner a sa route avec des waypoints ordonnés
- [ ] **Merge point** : les routes convergent avant l'objectif
- [ ] **Emplacements défensifs** : 10-14, avec `tower_id` unique et `max_triggers` varié
- [ ] **Verticalité** : au moins 2 HighGround platforms avec ramp access
- [ ] **Switchback** : au moins 1 section permettant le multi-segment coverage
- [ ] **Pacing spatial** : alterner corridors (killzones) et open arenas (skill expression)
- [ ] **Chokepoints** : 2-3 par lane, distribués le long du path
- [ ] **Objectif** : centré, taille suffisante, health défini
- [ ] **Path length** : 60+ cells de dwell time (niveau d'intro)
- [ ] **NavMesh** : le `NavMeshSurface` a un layer mask excluant l'UI

---

## Référence IntGrid

| Value | ID | Couleur | Blockout 3D | Usage |
|-------|----|---------|-------------|-------|
| 0 | (vide) | — | Rien | Bounding, vide |
| 1 | Floor | Vert `#4CAF50` | Cube (1, 0.2, 1) | Ground level praticable |
| 2 | Wall | Rouge `#F44336` | Cube (1, 3, 1) | Hard cover / LoS blocker (3m) |
| 3 | Ramp | Orange `#FF9800` | Escalier auto-généré | Transition ground ↔ HighGround |
| 5 | HighGround | Bleu `#2196F3` | Cube (1, 2, 1) | Elevated platform (2m) |

### Tips LDtk

- Blockout les walls en premier pour définir le *flow* de la map
- Poser le floor ensuite pour les paths et les arenas
- HighGround + Ramps en dernier (vérifier l'adjacence pour la génération auto)
- Les entités sont placées à la hauteur du terrain sous elles automatiquement (postprocesseur)
- Itérer souvent : sauvegarder dans LDtk → Unity reimport → playtest

---

## Ressources externes

### Articles fondamentaux

- **[Kingdom Rush — Campaign Level Design Analysis](https://www.gamedeveloper.com/design/kingdom-rush---the-wonderful-campaign-level-design)** — Déconstruction du level design de Kingdom Rush. Couvre les killzones, le placement stratégique, et la progression de campagne. Référence absolue en TD.

- **[Optimizing Tower Defense for Focus and Thinking (Defender's Quest)](https://www.fortressofdoors.com/optimizing-tower-defense-for-focus-and-thinking-defenders-quest/)** — Philosophie de design : garder le joueur en état de flow, éviter la surcharge cognitive.

- **[Tower Defense Game Rules, Part 1 (Gamedeveloper)](https://www.gamedeveloper.com/design/tower-defense-game-rules-part-1-)** — Fondamentaux du genre : économie, pathing, placement.

- **[Combining Tower Defense with a Shooter (Gamedeveloper)](https://www.gamedeveloper.com/design/combining-tower-defense-with-a-shooter---game-design-implications)** — Analyse spécifique au hybride TD+FPS. Tensions entre les deux loops, solutions par le level design.

### Postmortems

- **[Postmortem: Sanctum 2 (Coffee Stain Studios)](https://www.gamedeveloper.com/business/postmortem-sanctum-2)** — La référence la plus proche d'AIWE. Retour d'expérience sur le maze building en FPS, le partage de ressources en co-op, et le passage de 20-30 waves à ~10.

- **[Sanctum 2 Developer Level Design Diary](https://www.gamewatcher.com/news/2013-08-05-sanctum-2-developer-diary-level-design)** — Focus sur la verticalité et les itérations de level design.

- **[Orcs Must Die! Deathtrap — Map Philosophy](https://steamcommunity.com/app/1522820/discussions/0/3034850940607944303/)** — Discussion communautaire sur ce qui fait une bonne map OMD. Focus killzones et multi-lane.

### Guides techniques

- **[Flow Field Pathfinding for Tower Defense (Red Blob Games)](https://www.redblobgames.com/pathfinding/tower-defense/)** — Référence technique sur le pathfinding TD. Comprendre comment les ennemis naviguent.

- **[Engineering Tower Defense Games (Game-Ace)](https://game-ace.com/blog/engineering-of-tower-defense-games/)** — Vue d'ensemble technique : économie, progression, UX.

- **[Creating a Tower Defense Map (TheHelper)](https://world-editor-tutorials.thehelper.net/towerdef.php)** — Guide pratique de conception de map TD.

### Analyses de jeux

- **[Defense Grid — The Gemsbok Analysis](https://thegemsbok.com/art-reviews-and-articles/mid-week-mission-defense-grid-awakening-hidden-path/)** — Analyse de Defense Grid. Comprendre le mazing et l'optimisation de paths.

- **[Sanctum 2 Game Design Review](https://dshen6.github.io/posts/sanctum-2-game-design-review)** — Review approfondie : ce qui fonctionne et ce qui ne fonctionne pas dans un FPS TD co-op.

- **[Dungeon Defenders General Strategies (DadsGamingAddiction)](http://www.dadsgamingaddiction.com/dungeon-defenders-general-strategies/)** — Stratégies émergentes du level design de Dungeon Defenders. Comment les joueurs exploitent la géographie.

### Vidéos

- **[GDC: The Art of Tower Defense](https://www.youtube.com/results?search_query=gdc+tower+defense+design)** — Talks GDC sur le TD design
- **[Game Maker's Toolkit: What Makes a Good Tower Defense?](https://www.youtube.com/results?search_query=game+makers+toolkit+tower+defense)** — Analyses accessibles du genre

---

## Annexe : Anatomie du niveau "Foundry"

Le premier niveau applique les principes de ce guide :

```
Zone            | Rows  | Design intent
────────────────────────────────────────────────────
Dual Spawn      | 0-5   | 2 spawners flancs opposés → lane split
Dual Corridors  | 6-16  | Killzone par lane + overwatch central
Merge Arena     | 17-23 | Convergence, AoE premium, open arena
Switchback      | 24-35 | Multi-segment coverage depuis un seul slot
Final + Obj     | 36-45 | Last stand, arena ouverte
```

**Pourquoi ce layout fonctionne :**
- Le dual-lane force le split entre joueurs (co-op)
- Le HighGround central offre une overwatch position premium couvrant les deux lanes
- Le switchback récompense la lecture de map (multi-segment coverage)
- L'alternance corridors / open arenas maintient le skill expression FPS
- Le merge point crée un moment de tension partagée
- Le path est long (~70 cells de dwell time) → tolérant pour un premier niveau
