# AIWE — Game Design Document

## Concept

Tower Defense isométrique solo sur grille. Le joueur place jusqu'à 5 tours sur une grille et les configure via un **éditeur visuel de nodes** (style Scratch). Chaque tour est un châssis vide dont le comportement est entièrement défini par les modules branchés dans l'éditeur.

Inspiration : Bloons TD, Kingdom Rush, Geometry Wars (esthétique).

## Twist principal

Les tours sont des **châssis vides** configurables via un **éditeur de nodes**. Tout repose sur **3 types de modules** :

| Type | Rôle | Exemple |
|---|---|---|
| **Trigger** | QUAND ça se déclenche | On Timer, On Enemy Enter Range |
| **Zone** | OÙ / QUI est ciblé | Nearest Enemy, All In Range |
| **Effect** | CE QUE ça fait | Projectile, Hitscan, Slow, DOT |

---

## Architecture du graphe

L'éditeur se lit de gauche à droite :

```
TRIGGER (QUAND) ──► ZONE (OÙ/QUI) ──► EFFECT (QUOI)
                     Zone ──► Zone (chaînage série)
                     Effect ──► Effect (chaînage vertical)
```

### Flux d'exécution

1. Un **trigger** se déclenche (ex: timer toutes les 2s)
2. Les **zones** se résolvent de gauche à droite, chacune sélectionne ses cibles
3. Pour chaque zone, les **effets** s'exécutent de haut en bas sur les cibles sélectionnées
4. Ordre : Zone A (effets ↓↓↓), puis Zone B (effets ↓↓↓)...

### Règles

- Chaque châssis a un **nombre max de triggers** (Sentinelle = 3)
- Les zones se chaînent horizontalement (pas de limite)
- Les effets s'empilent verticalement sous chaque zone (pas de limite)
- Max chain depth = 8

---

## Modules

### Triggers

| Trigger | Description |
|---|---|
| `On Timer (Xs)` | Toutes les X secondes |
| `On Enemy Enter Range` | Un ennemi entre dans le rayon de détection |
| `On Enemy Exit Range` | Un ennemi quitte le rayon |

### Zones

| Zone | Sélection |
|---|---|
| `Nearest Enemy` | L'ennemi le plus proche |
| `All Enemies In Range` | Tous les ennemis dans le rayon (AoE) |
| `Weakest Enemy` | L'ennemi avec le moins de HP |
| `Strongest Enemy` | L'ennemi avec le plus de HP |
| `Random Enemy` | Un ennemi aléatoire dans le rayon |
| `First In Path` | L'ennemi le plus avancé vers l'objectif |
| `Last In Path` | L'ennemi le plus loin de l'objectif |
| `Highest Health %` | L'ennemi avec le plus de % de HP |
| `Lowest Health %` | L'ennemi avec le moins de % de HP |
| `Self Target` | La tour elle-même |
| `Forward Aim` | Direction de tir de la tour |

### Effets

| Effet | Description |
|---|---|
| `Projectile` | Tire un projectile physique |
| `Hitscan` | Rayon instantané |
| `Slow` | Ralentit la cible |
| `DOT` | Dégâts sur la durée |
| `Knockback` | Repousse la cible |
| `Weaken` | +% dégâts subis |
| `Vulnerability` | Augmente la vulnérabilité |
| `Speed Boost` | Boost de vitesse (buff allié) |
| `Damage Boost` | Boost de dégâts (buff allié) |
| `Heal` | Soigne la cible |

---

## Placement de tours

- **5 tours maximum** par partie
- Posables sur toute case `Empty` ou `TowerSlot` de la grille
- Pas posable sur : `Path`, `Spawn`, `Objective`, `Blocked`
- Chaque tour posée reçoit un **loadout par défaut** : `On Timer → Nearest Enemy → Projectile`
- Cliquer sur une tour posée ouvre l'éditeur de nodes pour la reconfigurer
- Le placement n'est actif que pendant la phase `Preparing`

---

## Ennemis

Les ennemis sont des tétraèdres qui suivent un chemin prédéfini sur la grille, segment par segment. Ils ne tournent pas quand ils changent de direction. Quand ils atteignent l'objectif, ils infligent des dégâts.

### IA

State machine à 4 états : `FollowRoute → ChaseTarget → Attack → ReturnToRoute`

Les ennemis évaluent un **score de menace** pour chaque tour à portée. S'ils détectent une menace suffisante, ils quittent le path pour attaquer la tour, puis reviennent au waypoint le plus proche.

### Paramètres par type (`EnemyDefinition`)

| Paramètre | Rôle |
|-----------|------|
| `maxHP` | Points de vie |
| `moveSpeed` | Vitesse de déplacement |
| `attackDamage` | Dégâts par coup |
| `attackCooldown` | Temps entre deux coups |
| `attackRange` | Portée melee |
| `detectionRadius` | Rayon de détection des menaces |
| `leashRadius` | Distance max avant de-aggro |
| `objectiveDamage` | Dégâts infligés à l'objectif |
| `color` | Couleur de l'ennemi |
| `shape` | Forme géométrique (Triangle, Diamond, etc.) |

---

## Économie & Loot

### Système de drop (Loot Table)

Les modules s'obtiennent via des **drops sur les ennemis tués**. Chaque type d'ennemi possède sa propre **Loot Table** (ScriptableObject).

1. **Roll de rareté** — On tire d'abord quelle rareté tombe (Common, Uncommon, Rare, Epic)
2. **Roll de module** — On pioche un module au hasard parmi le pool de cette rareté

Les pickups volent automatiquement vers le centre de l'écran (magnet) et s'ajoutent à l'inventaire.

---

## Game loop

```
Phase Preparing
  ├─ Placer des tours (max 5)
  ├─ Configurer les tours via le node editor
  └─ Cliquer "Start Wave"

Phase Wave
  ├─ Ennemis spawn et suivent le path
  ├─ Tours tirent automatiquement
  ├─ Ennemis meurent → drop modules
  └─ Tous les ennemis éliminés → retour Preparing

Game Over
  └─ HP de l'objectif tombe à 0
```

---

## Châssis

| Châssis | Max triggers | Portée | Arc de tir |
|---|---|---|---|
| Sentinelle | 3 | 10 | 360° |

---

## Décisions prises

- Jeu **solo** (pas de co-op, pas de multijoueur)
- Vue **isométrique** (30° pitch, 45° yaw, orthographique)
- Gameplay sur **grille** (tout est grid-snapped)
- **5 tours max** par partie
- Ennemis suivent des **paths prédéfinis** sur la grille (pas de pathfinding dynamique)
- Loadout par défaut sur chaque tour (OnTimer→NearestEnemy→Projectile)
- Pas de joueur FPS sur le terrain
- L'éditeur de nodes est accessible pendant la phase Preparing
- Visuels : URP/Lit materials, Bloom post-process, formes géométriques simples

## Questions ouvertes

- Nombre de vagues par partie ?
- Types d'ennemis variés (rapides, tanks, volants) ?
- Système d'upgrade des tours (niveaux) ?
- Meta-progression entre les parties ?
- Coût en ressources pour les tours ?
