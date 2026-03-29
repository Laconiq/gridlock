# Gridlock — Game Design Document

## Concept

Tower Defense isometrique solo sur grille. Le joueur defend un objectif contre des vagues d'ennemis en placant des tours et en configurant leurs **projectiles** via un systeme de **Mod Slots** — une rangee de slots ou chaque mod empile change le comportement de la balle.

Inspiration : Bloons TD, Kingdom Rush (TD), Noita (wand slots), Q-UP (adjacency synergies), Balatro (ordre = strategie), Binding of Isaac (flags empilees), Geometry Wars (esthetique neon + grid deformation).

## Twist principal

Les tours sont des chassis qui tirent automatiquement. Le joueur ne programme pas le **comportement** de la tour — il **construit le projectile** en placant des mods dans des slots. L'ordre des mods et leur voisinage determinent le comportement de la balle.

**Une seule regle a comprendre :** les mods avant un Event modifient le projectile. Les mods apres un Event creent un sous-projectile qui spawn a l'event.

---

## Systeme de Mod Slots

### Vue d'ensemble

Chaque tour a une **barre horizontale de slots**. Le joueur drag & drop des mods dans les slots depuis son inventaire. Pas de cables, pas de graph — juste des pieces dans des cases.

```
┌───────────────────────────────────────────────────────┐
│  TOUR T2         [First ▼]  ◈ 5 slots                │
│                                                       │
│  [ Homing ] [ Heavy ] [ ⟐ Hit ] [ Split ] [ Burn ]   │
│      1         2         3         4        5         │
└───────────────────────────────────────────────────────┘
```

### Lecture de la chaine

La chaine se lit **gauche → droite** comme une phrase :

```
[ Homing ] [ Heavy ] [ ⟐ Hit ] [ Split ] [ Burn ]
├── projectile ──┤    event   ├── spawn ────────┤
```

*"Tire une balle homing lourde. A l'impact, split en 3 eclats enflammes."*

### Regle fondamentale

Les **Events** (losanges ⟐) divisent la chaine en etapes :
- **Avant l'event** = traits du projectile principal
- **Apres l'event** = traits du sous-projectile qui spawn a l'event

Sans event dans la chaine, tous les mods stackent simplement sur la balle.

### L'ordre compte

Memes mods, ordre different, tour differente :

```
[ Homing ] [ Heavy ] [ ⟐ Hit ] [ Split ] [ Burn ]
= Balle lourde guidee → a l'impact, 3 eclats de feu

[ Split ] [ Burn ] [ ⟐ Hit ] [ Homing ] [ Heavy ]
= 3 balles enflammees → a l'impact de chacune, sous-balle lourde guidee
```

### Events chainables

On peut enchainer plusieurs events pour creer des cascades :

```
[ Homing ] [ ⟐ Hit ] [ Split ] [ ⟐ Kill ] [ Wide ] [ Burn ]
```
*"Balle homing. Impact → 3 eclats. Si un eclat tue → explosion de zone enflammee."*

Max **3 etages** d'events (pour la balance).

---

## Catalogue de Mods

### Traits (12)

Chaque trait fait UNE chose claire. Pas de stats a tuner, pas de sous-menus.

#### Comportement

| Mod | Effet | Details |
|-----|-------|---------|
| **Homing** | La balle cherche la cible | Courbe vers l'ennemi le plus proche dans un cone |
| **Pierce** | Traverse les ennemis | Continue sa trajectoire apres impact (max 3 par defaut) |
| **Bounce** | Rebondit entre ennemis | Saute a l'ennemi le plus proche apres impact (max 3 par defaut) |
| **Split** | Se divise en 3 | A la fin de vie ou a l'event, spawn 3 sous-projectiles en eventail |
| **Heavy** | +degats, +gros, -vitesse | x2 damage, x1.5 size, x0.6 speed |
| **Swift** | +vitesse, +cadence, -degats | x1.8 speed, x0.8 fire interval, x0.6 damage |
| **Wide** | Impact en zone | Mini AOE a l'impact (rayon = 2 unites) |

#### Elementaire

| Mod | Effet | Details |
|-----|-------|---------|
| **Burn** | DOT feu | 5 dmg/s pendant 3s. Applique le statut "Burning" |
| **Frost** | Slow | -50% vitesse pendant 2s. Applique le statut "Frozen" |
| **Shock** | Chaine electrique | Saute a 1 ennemi proche (rayon 3). Applique le statut "Shocked" |
| **Void** | % HP courant | Inflige 8% des HP courants de la cible. Ignore l'armure (futur) |
| **Leech** | Drain de vie | Soigne l'objectif de 20% des degats infliges |

### Events (14)

Les events sont les "virgules" de la phrase. Ils divisent la chaine en etapes.

#### Basiques (disponibles de base)

| Event | Declencheur |
|-------|-------------|
| **⟐ Hit** | A chaque impact sur un ennemi |
| **⟐ Kill** | Quand le projectile tue un ennemi |
| **⟐ End** | Quand le projectile meurt (fin de portee/lifetime) |

#### Mod-dependants (droppent avec le mod associe)

Ces events ne se declenchent **que si le mod correspondant est present AVANT eux dans la chaine**. Le joueur decouvre cette regle naturellement.

| Event | Mod requis avant | Declencheur |
|-------|-----------------|-------------|
| **⟐ Pierce** | `Pierce` | Chaque fois que la balle traverse un ennemi |
| **⟐ Bounce** | `Bounce` | A chaque rebond entre ennemis |
| **⟐ Chain** | `Shock` | Quand l'eclair saute a un ennemi voisin |

#### Temporels

| Event | Declencheur | Details |
|-------|-------------|---------|
| **⟐ Delay** | Apres X secondes de vol | Default 0.5s. Timer de Noita — bombes a retardement |
| **⟐ Pulse** | Toutes les X secondes pendant la vie du projectile | Default 0.3s. Cree des balles "vivantes" qui emettent |

#### Conditionnels (synergies inter-tours)

Ces events checkent l'etat de l'ennemi touche. C'est ce qui transforme les tours en EQUIPE.

| Event | Condition sur la cible |
|-------|----------------------|
| **⟐ If-Burning** | La cible a le statut Burning |
| **⟐ If-Frozen** | La cible a le statut Frozen |
| **⟐ If-Shocked** | La cible a le statut Shocked |
| **⟐ If-Low** | La cible est en dessous de 30% HP |

#### Meta / Rares (drops rares, late-game)

| Event | Declencheur |
|-------|-------------|
| **⟐ Crit** | Sur un coup critique (10% de base) |
| **⟐ Overkill** | Si les degats depassent les HP restants de la cible |

### Total : 12 traits + 14 events = 26 pieces

---

## Combos de voisinage

Inspire de Q-UP : deux mods **adjacents** dans les slots creent une **synergie bonus**. Le joueur les decouvre en experimentant.

| Voisins | Synergie | Effet bonus |
|---------|----------|-------------|
| Heavy + Heavy | **Railgun** | Gagne Pierce gratuit |
| Swift + Swift | **Machinegun** | Fire rate doublee en plus |
| Frost + Frost | **Blizzard** | Le slow devient un freeze (stun 0.5s) |
| Shock + Shock | **Tesla** | Chain a 3 ennemis au lieu de 1 |
| Burn + Wide | **Napalm** | Zone de feu persistante au sol (3s) |
| Frost + Wide | **Avalanche** | Zone de slow persistante (3s) |
| Pierce + Bounce | **Ricochet** | Rebonds et pierce illimites pendant 2s |
| Homing + Swift | **Missile** | Homing parfait (snap direct, pas de courbe) |
| Heavy + Wide | **Meteor** | AOE x2 rayon, screen shake a l'impact |
| Burn + Frost | **Thermal Shock** | Degats burst = 50% HP max (consume les deux statuts) |
| Shock + Void | **Siphon** | Les degats void soignent l'objectif |
| Split + Split | **Barrage** | x5 au lieu de x3+x3 |
| Leech + Heavy | **Vampire** | Drain 40% au lieu de 20% |

---

## Targeting

Le targeting est un **selecteur simple** sur chaque tour (dropdown ou toggle) :

| Mode | Selection |
|------|-----------|
| **First** | Ennemi le plus avance vers l'objectif |
| **Nearest** | Ennemi le plus proche de la tour |
| **Strongest** | Ennemi avec le plus de HP |
| **Weakest** | Ennemi avec le moins de HP |
| **Last** | Ennemi le plus loin de l'objectif |

---

## Tours

### Chassis

Chaque tour est un chassis avec des stats fixes :

| Stat | Description |
|------|-------------|
| `baseRange` | Portee de detection/tir |
| `fireRate` | Intervalle de tir (secondes) |
| `baseDamage` | Degats de base du projectile (avant mods) |
| `slotCount` | Nombre de slots disponibles |

### Progression des slots

| Tier | Slots | Comment |
|------|-------|---------|
| Tier 1 | 3 slots | Tour de base |
| Tier 2 | 5 slots | Upgrade |
| Tier 3 | 7 slots | Upgrade |

La contrainte de slots force des choix significatifs : build simple mais puissant (3 traits empiles) ou build complexe avec events et sous-projectiles (qui mangent des slots).

---

## Ennemis

Les ennemis sont des tetraedres qui suivent un chemin predefini sur la grille, segment par segment. Quand ils atteignent l'objectif, ils infligent des degats.

### Comportement

**TD classique** : les ennemis suivent le path segment par segment jusqu'a l'objectif. Pas d'IA de combat, pas de ciblage de tours.

### Parametres par type (`EnemyDefinition`)

| Parametre | Role |
|-----------|------|
| `maxHP` | Points de vie |
| `moveSpeed` | Vitesse de deplacement |
| `objectiveDamage` | Degats infliges a l'objectif |
| `color` | Couleur de l'ennemi |
| `shape` | Forme geometrique |

### Statuts applicables

| Statut | Source | Effet |
|--------|--------|-------|
| **Burning** | Mod `Burn` | DOT 5 dmg/s, 3s |
| **Frozen** | Mod `Frost` | -50% speed, 2s |
| **Shocked** | Mod `Shock` | Pret a chainer, 2s |

### Effets visuels

- **Hit flash** : emission blanche sur impact
- **Damage text** : nombres flottants
- **Death** : explosion voxel (mesh desintegre en cubes physiques)
- **Grid warp** : onde coloree qui se propage sur la grille au hit/kill

---

## Economie & Loot

### Systeme de drop

Les mods s'obtiennent via des **drops sur les ennemis tues**. Chaque type d'ennemi possede sa propre **Loot Table**.

1. **Roll de rarete** — Common, Uncommon, Rare, Epic
2. **Roll de mod** — Un mod au hasard parmi le pool de cette rarete

| Rarete | Mods typiques |
|--------|--------------|
| Common | Homing, Pierce, Heavy, Swift, Burn, Frost |
| Uncommon | Bounce, Split, Wide, Shock, Events basiques |
| Rare | Void, Leech, Events mod-dependants, Events temporels |
| Epic | Events conditionnels, Events meta |

Les pickups volent automatiquement vers le centre de l'ecran (magnet) et s'ajoutent a l'inventaire.

---

## Game loop

```
Phase Preparing
  ├─ Placer des tours
  ├─ Configurer les mod slots de chaque tour (drag & drop)
  ├─ Choisir le targeting mode par tour
  └─ Cliquer "Start Wave"

Phase Wave
  ├─ Ennemis spawn et suivent le path
  ├─ Tours tirent automatiquement selon leur mod chain
  ├─ Ennemis meurent → drop mods
  └─ Tous les ennemis elimines → retour Preparing

Game Over
  └─ HP de l'objectif tombe a 0
```

---

## UI du Mod Slot Editor

### Layout

Le panel s'ouvre quand le joueur clique sur une tour. Pas de canvas infini — tout tient dans un panel.

```
┌─────────────────────────────────────────────────────────────┐
│  ◈ TOWER_02         Range: 10     Fire Rate: 2.0s          │
│  Targeting: [First ▼]                                       │
│                                                             │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐             │
│  │Homing│→│Heavy │→│⟐ Hit │→│Split │→│ Burn │             │
│  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘             │
│       ↑ RAILGUN ↑                                           │
│                                                             │
│  ─── INVENTORY ───────────────────────────────────────────  │
│  [Homing x3] [Pierce x2] [Frost x1] [⟐ Kill x1] ...      │
│                                                             │
│              [ SAVE ]  [ CANCEL ]                           │
└─────────────────────────────────────────────────────────────┘
```

- Les synergies de voisinage s'affichent entre les deux mods concernes (glow + nom)
- L'inventaire montre les mods disponibles avec leur count
- Drag & drop pour placer, swap en dragant sur un slot occupe

---

## Decisions prises

- Jeu **solo** (pas de co-op, pas de multijoueur)
- Vue **isometrique** (30° pitch, 45° yaw, orthographique)
- Gameplay sur **grille** (tout est grid-snapped)
- Tours tirent automatiquement, le joueur configure le projectile
- Targeting = dropdown simple (First, Nearest, Strongest, Weakest, Last)
- Systeme de **Mod Slots** (remplace le node graph avec cables)
- Les mods sont les drops principaux
- Max 3 events chainables dans une chaine de slots
- Visuels : esthetique neon (Geometry Wars), Bloom/chromatic aberration, grid warp mass-spring, voxel death effects, screen shake/freeze frame

## Questions ouvertes

- Nombre max de tours ?
- Nombre de vagues par partie ?
- Types d'ennemis varies (rapides, tanks, volants) ?
- Systeme d'upgrade de tier des tours (comment on passe T1→T2→T3) ?
- Meta-progression entre les parties ?
- Cout en ressources pour les tours ?
- Les combos de voisinage sont-ils documentes in-game ou uniquement decouverts ?
