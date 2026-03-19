# AIWE — Game Design Document

## Concept

FPS Tower Defense coopératif en ligne (2-4 joueurs). Les joueurs défendent une zone contre des vagues d'ennemis en combinant combat FPS et placement de défenses modulaires.

Inspiration principale : Orc Must Die, AIVI, Q-UP.

## Twist principal

Les tourelles et armes sont des **châssis vides** configurables via un **éditeur visuel de nodes** (style Scratch). Tout repose sur **3 types de modules** :

| Type | Rôle | Position dans l'éditeur |
|---|---|---|
| **Trigger** | QUAND ça se déclenche | Colonne de gauche, empilés verticalement |
| **Zone** | OÙ / QUI est ciblé | Chaîne horizontale, de gauche à droite |
| **Effet** | CE QUE ça fait | Rack vertical sous chaque Zone |

Les mêmes modules Zone et Effet marchent sur les tourelles et les armes du joueur.

---

## Architecture du graphe

### Diagramme de l'éditeur

L'éditeur se lit dans 3 directions :
- **↓ Vertical gauche** : les triggers (QUAND)
- **→ Horizontal** : les zones (OÙ/QUI)
- **↓ Vertical sous chaque zone** : les effets (CE QUE ÇA FAIT)

```
  TRIGGERS                ZONES ──────────────────────────────────────▶
  (QUAND)                 (OÙ/QUI)
     │
     │         ┌─────────────────┐      ┌─────────────────┐      ┌──────────────┐
     │         │ 🎯 ZONE         │      │ 🎯 ZONE         │      │ 🎯 ZONE      │
     ▼    ┌───▶│ All In Range    │─────▶│ Nearest Ally    │─────▶│ Self         │──▶ ...
 ┌────────┤    │                 │      │ Turret          │      │              │
 │⚡TRIGGER│    └────────┬────────┘      └────────┬────────┘      └──────┬───────┘
 │On Enemy│             │                        │                      │
 │Enter   │             ▼                        ▼                      ▼
 │Range   │    ┌─────────────────┐      ┌─────────────────┐      ┌──────────────┐
 └────────┘    │ ✦ Vortex        │      │ ✦ Boost Damage  │      │ ✦ Shield     │
               ├─────────────────┤      ├─────────────────┤      └──────────────┘
               │ ✦ Slow          │      │ ✦ Energize      │
               ├─────────────────┤      └─────────────────┘
               │ ✦ Weaken        │
               ├─────────────────┤       EFFETS
               │ ✦ Mark          │       (CE QUE ÇA FAIT)
               └─────────────────┘            │
                      │                       │
                      ▼                       ▼

 ┌────────┐    ┌─────────────────┐      ┌─────────────────┐
 │⚡TRIGGER│    │ 🎯 ZONE         │      │ 🎯 ZONE         │
 │On Kill │───▶│ All Ally Turrets│─────▶│ Nearest Player  │──▶ ...
 │        │    │ In Range        │      │                 │
 └────────┘    └────────┬────────┘      └────────┬────────┘
                        │                        │
                        ▼                        ▼
               ┌─────────────────┐      ┌─────────────────┐
               │ ✦ Signal        │      │ ✦ Boost Damage  │
               └─────────────────┘      └─────────────────┘


 ┌────────┐    ┌─────────────────┐
 │⚡TRIGGER│    │ 🎯 ZONE         │
 │On      │───▶│ Marked Enemy    │──▶ ...
 │Trigger │    │                 │
 └────────┘    └────────┬────────┘
                        │
                        ▼
               ┌─────────────────┐
               │ ✦ Amplify       │
               ├─────────────────┤
               │ ✦ Mortar        │
               ├─────────────────┤
               │ ✦ Overload      │
               └─────────────────┘
```

### Légende

```
⚡ = Trigger   (QUAND)     — colonne de gauche, empilés verticalement
🎯 = Zone      (OÙ/QUI)    — chaîne horizontale vers la droite
✦  = Effet    (FAIT QUOI)  — rack vertical sous chaque zone
```

### Flux d'exécution

1. Un **trigger** se déclenche (ex: un ennemi entre dans le rayon)
2. Les **zones** se résolvent de gauche à droite, chacune sélectionne sa cible
3. Pour chaque zone, le **rack d'effets** s'exécute de haut en bas sur la cible sélectionnée
4. Ordre : Zone A (effet ↓↓↓), puis Zone B (effet ↓↓↓), puis Zone C (effet ↓↓↓)...

### Règles

- Les **triggers** s'empilent verticalement à gauche (limité par le châssis)
- Les **zones** se chaînent horizontalement depuis chaque trigger (pas de limite)
- Les **effets** s'empilent verticalement sous chaque zone (pas de limite)
- Chaque châssis a un **nombre max de triggers**

---

## Modules : Triggers

Le trigger répond à **QUAND** — quel événement déclenche la chaîne.

### Triggers tourelle

| Trigger | Description |
|---|---|
| `On Enemy Enter Range` | Un ennemi entre dans le rayon de détection |
| `On Enemy Exit Range` | Un ennemi quitte le rayon |
| `On Enemy In Range` | Tant qu'un ennemi est dans le rayon (tick continu) |
| `On Damage Taken` | La tourelle reçoit des dégâts |
| `On Destroyed` | La tourelle est détruite (dernier souffle) |
| `On Ally Turret Fire` | Une tourelle alliée à proximité tire |
| `On Ally Turret Destroyed` | Une tourelle alliée proche est détruite |
| `On Player Nearby` | Un joueur allié est à proximité |
| `On Player Leave` | Un joueur allié quitte la zone |
| `On Kill` | La tourelle tue un ennemi |
| `On Wave Start` | Une nouvelle vague commence |
| `On Wave End` | La vague est terminée |
| `On Timer (Xs)` | Toutes les X secondes |
| `On HP Below X%` | Les HP de la tourelle passent sous un seuil |
| `On Marked Enemy In Range` | Un ennemi marqué entre dans le rayon |
| `On Trigger` | Activé par un `Signal` d'une autre tourelle (à la Q-UP) |

### Triggers joueur

| Trigger | Input |
|---|---|
| `Primary Fire` | Click gauche |
| `Secondary Fire` | Click droit |

---

## Modules : Zones

La zone répond à **OÙ / QUI** — quelle cible ou quelle zone est affectée. Les effets branchés en dessous s'appliquent à la sélection de cette zone.

### Zones ennemis

| Zone | Sélection |
|---|---|
| `Nearest Enemy` | L'ennemi le plus proche |
| `Farthest Enemy` | L'ennemi le plus loin dans le rayon |
| `Strongest Enemy` | L'ennemi avec le plus de HP |
| `Weakest Enemy` | L'ennemi avec le moins de HP |
| `Most Armored` | L'ennemi avec le plus d'armure |
| `Fastest Enemy` | L'ennemi le plus rapide |
| `Slowest Enemy` | L'ennemi le plus lent |
| `Marked Enemy` | Un ennemi marqué (ignore les non-marqués) |
| `Random Enemy` | Un ennemi aléatoire dans le rayon |
| `All Enemies In Range` | Tous les ennemis dans le rayon (AoE) |

### Zones alliés

| Zone | Sélection |
|---|---|
| `Self` | La tourelle/joueur source |
| `Nearest Ally Turret` | La tourelle alliée la plus proche |
| `All Ally Turrets In Range` | Toutes les tourelles alliées dans le rayon |
| `Nearest Player` | Le joueur allié le plus proche |
| `All Players In Range` | Tous les joueurs alliés dans le rayon |
| `Linked Turret` | La tourelle liée (via effet `Link`) |

### Zones spéciales

| Zone | Sélection |
|---|---|
| `Same As Previous` | La même cible que la zone précédente dans la chaîne |
| `Ground Area` | Une zone au sol (placement d'un champ persistant) |
| `Projectile Path` | Le long de la trajectoire du projectile |

---

## Modules : Effets

L'effet répond à **CE QUE ÇA FAIT** — l'action concrète. Les effets s'exécutent de haut en bas dans le rack, séquentiellement, sur la cible de leur zone.

### Dégâts

| Effet | Description |
|---|---|
| `Projectile` | Tire un projectile physique (vitesse, gravité) |
| `Hitscan` | Rayon instantané, précis |
| `Shotgun Blast` | Plusieurs projectiles en cône |
| `Beam` | Rayon continu, dégâts croissants sur la même cible |
| `Mortar` | Tir en cloche, AoE à l'impact |
| `Melee Swipe` | Dégâts en arc autour de la source |
| `Lightning Arc` | Décharge qui saute entre X ennemis proches |
| `Laser Burst` | Salve de 3 rayons rapides |
| `Explosion` | Dégâts AoE instantanés centrés sur la cible |
| `DoT Field` | Zone de dégâts persistante au sol (nécessite zone `Ground Area`) |

### Mouvement

| Effet | Description |
|---|---|
| `Slow` | Ralentit la cible (-X%) |
| `Knockback` | Repousse la cible |
| `Pull` | Attire la cible vers la source |
| `Root` | Immobilise pendant X secondes |
| `Launch` | Envoie en l'air (dégâts de chute) |
| `Conveyor` | Pousse dans une direction configurable |
| `Vortex` | Aspire les ennemis proches de la cible vers elle |
| `Teleport` | Renvoie la cible au début de son chemin |

### Debuffs

| Effet | Description |
|---|---|
| `Mark` | Tag la cible → active les `On Marked Enemy` des autres tourelles |
| `Weaken` | +X% dégâts subis de toutes sources |
| `Blind` | Perd l'aggro, erre aléatoirement |
| `Corrode` | DoT + réduit l'armure au fil du temps |
| `Overload` | Explose à sa mort, AoE aux ennemis proches |
| `Leash` | Attache la cible à la source (portée limitée) |
| `Jam` | Désactive les capacités spéciales de la cible |
| `Curse` | Les dégâts subis sont partagés aux ennemis proches |
| `Magnetize` | Les projectiles alliés sont attirés vers la cible |
| `Fragile` | Le prochain hit fait ×3 dégâts |
| `Combustion` | Prend feu si touchée à nouveau dans les Xs |
| `Echo` | Le dernier effet subi se répète après Xs |
| `Chain Link` | Lie les dégâts à l'ennemi le plus proche (damage bridge) |
| `Time Bomb` | Après Xs, subit tous les dégâts accumulés d'un coup |

### Buffs (quand la zone cible un allié)

| Effet | Description |
|---|---|
| `Boost Fire Rate` | +X% cadence de tir |
| `Boost Damage` | +X% dégâts |
| `Boost Range` | +X% portée |
| `Shield` | Bouclier temporaire, absorbe un hit |
| `Repair` | Soigne la tourelle |
| `Ammo Feed` | Réduit les cooldowns |
| `Copy` | Duplique le prochain tir de l'allié |
| `Energize` | Force un re-trigger immédiat de l'allié |
| `Overclock` | ×2 fire rate pendant Xs, mais prend des dégâts |
| `Infuse` | Le prochain tir de l'allié applique aussi les effets de cette tourelle |

### Relay (communication entre tourelles)

| Effet | Description |
|---|---|
| `Signal` | Envoie un signal → active le `On Trigger` des tourelles ciblées |
| `Repeat Relay` | Re-trigger toutes les tourelles liées |
| `Link` | Lie la source et la cible — les buffs s'appliquent aux deux |
| `Mirror` | La tourelle ciblée reproduit le même tir |
| `Redirect` | Les projectiles proches convergent vers la cible actuelle |

### Modifiers (modifient l'effet au-dessus ou en dessous dans le rack)

| Effet | Description |
|---|---|
| `Pierce` | Le projectile traverse les ennemis |
| `Split` | Le projectile se divise en X |
| `Ricochet` | Le projectile rebondit entre X ennemis |
| `Homing` | Le projectile traque sa cible |
| `Size Up` | ×2 zone d'effet / taille |
| `Amplify` | ×2 dégâts, ×2 cooldown |
| `Miniaturize` | ÷2 dégâts, ÷2 cooldown |
| `Bounce` | L'effet rebondit au plus proche après impact |
| `Delay` | Retarde le prochain effet de Xs |
| `Duration Up` | Effets de statut +X% durée |
| `Multi-target` | Le prochain effet touche X cibles |
| `Repeat` | L'effet précédent s'exécute X fois de plus |
| `Conditional: HP Below` | Le prochain effet ne proc que si cible < X% HP |
| `Conditional: Marked` | Le prochain effet ne proc que si cible marquée |
| `Conditional: Alone` | Le prochain effet ne proc que si cible isolée |

---

## Exemples de builds

### Tourelle "Gatling" (simple)

```
 ⚡ On Enemy In Range ──▶ 🎯 Nearest Enemy
                                  │
                              ✦ Miniaturize
                              ✦ Projectile
```

### Tourelle "Sniper"

```
 ⚡ On Marked Enemy ──▶ 🎯 Marked Enemy
                               │
                           ✦ Amplify
                           ✦ Hitscan
                           ✦ Fragile
                           ✦ Echo
```

### Tourelle "Piège aspirant"

```
 ⚡ On Enemy Enter ──▶ 🎯 All In Range ──▶ 🎯 Nearest Ally Turret
                             │                        │
                         ✦ Vortex               ✦ Boost Damage
                         ✦ Slow                 ✦ Energize
                         ✦ Weaken
```

### Tourelle "Chain Reactor" (cascade Q-UP)

```
 ⚡ On Trigger ──▶ 🎯 All In Range ──▶ 🎯 All Ally Turrets
                        │                       │
                    ✦ Overload               ✦ Signal ─ ─ ─▶ (active d'autres On Trigger)
                    ✦ Curse
```

### Tourelle "Bombe kamikaze"

```
 ⚡ On Destroyed ──▶ 🎯 All In Range ──▶ 🎯 Nearest Ally Turret
                           │                       │
                       ✦ Delay (1s)            ✦ Shield
                       ✦ Size Up
                       ✦ Explosion
                       ✦ Overload
                       ✦ Time Bomb
```

### Tourelle "Commander"

```
 ⚡ On Timer (3s) ──▶ 🎯 All Ally Turrets ──▶ 🎯 Nearest Player
                             │                        │
                       ✦ Boost Fire Rate         ✦ Boost Damage
                       ✦ Ammo Feed               ✦ Shield
                       ✦ Energize
```

### Tourelle "Infection"

```
 ⚡ On Enemy In Range ──▶ 🎯 Nearest Enemy ──▶ 🎯 Same As Previous
                                │                        │
                            ✦ Mark                 ✦ Combustion
                            ✦ Corrode              ✦ Chain Link
                            ✦ Weaken
```

### Joueur "Spotter"

```
 ⚡ Primary Fire ──▶ 🎯 Nearest Enemy
                          │
                      ✦ Hitscan
                      ✦ Mark
                      ✦ Magnetize

 ⚡ Secondary Fire ──▶ 🎯 Nearest Ally Turret
                             │
                         ✦ Energize
                         ✦ Infuse
```

### Joueur "Chaos"

```
 ⚡ Primary Fire ──▶ 🎯 Nearest Enemy ──▶ 🎯 All In Range
                          │                       │
                     ✦ Shotgun Blast          ✦ Knockback
                     ✦ Pierce                 ✦ Vortex
                     ✦ Combustion

 ⚡ Secondary Fire ──▶ 🎯 Nearest Enemy
                            │
                        ✦ Pull
                        ✦ Curse
                        ✦ Time Bomb
```

---

## Combo inter-tourelles : "Le couloir de la mort"

3 tourelles placées le long d'un couloir, liées par Signal :

```
TOURELLE A (entrée du couloir)

 ⚡ On Enemy Enter ──▶ 🎯 All In Range
                            │
                        ✦ Mark
                        ✦ Slow
                        ✦ Signal ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐
                                                                   │
TOURELLE B (milieu du couloir)                                     │
                                                                   ▼
 ⚡ On Trigger ──▶ 🎯 All In Range ──▶ 🎯 Same As Previous        (reçoit le Signal)
                        │                       │
                    ✦ Pull                 ✦ Chain Link
                    ✦ Weaken              ✦ Combustion


TOURELLE C (fond du couloir)

 ⚡ On Marked Enemy ──▶ 🎯 Marked Enemy
                              │
                          ✦ Amplify
                          ✦ Mortar
                          ✦ Overload
```

**Flux** : A mark + slow tous les ennemis, signal → B reçoit le trigger, pull + weaken, chain link + combustion → C détecte les marqués, mortar amplifié + overload → explosion en chaîne.

---

## Châssis

| Châssis | Emplacement | Max triggers | Particularité |
|---|---|---|---|
| Sentinelle | Sol | 3 | Arc de tir 360°, polyvalente |
| Murale | Mur | 2 | Couverture couloir, profil bas |
| Plafonnier | Plafond | 2 | Surplomb, hors de portée mêlée |
| Barricade | Sol | 1 | Bloque le passage, HP très élevés |
| Piédestal | Sol | 2 | Portée élevée, rotation lente |
| Joueur | Mobile | 2 (fixe) | Primary + Secondary Fire |

---

## Économie & tourelles

- Les tourelles coûtent des ressources pour être posées
- Pas de limite stricte — limité par les ressources disponibles
- Ressources : kill, bonus de fin de vague, ramassage sur la map
- Les modules s'achètent / se droppent — inventaire partagé entre joueurs
- L'éditeur de nodes est accessible **tout le temps**

---

## Synergie joueur/tourelle

L'arme du joueur = châssis mobile avec `Primary Fire` et `Secondary Fire`, mêmes modules Zone et Effet.

### Interactions

- **Même modules** : un Knockback marche pareil sur tourelle ou arme
- **Mobilité** : le joueur Mark un ennemi, court vers la zone où les tourelles sont configurées pour les Marked
- **Proximity** : les tourelles qui ciblent `Nearest Player` buffent/shield le joueur à côté
- **Transfert** : retirer un module d'une tourelle ↔ arme en temps réel
- **Infuse** : greffer les effets de l'arme sur une tourelle temporairement
- **Signal** : le joueur peut trigger les tourelles configurées en `On Trigger`

### Dynamique coop

- Joueur Spotter (Mark + Magnetize) + Joueur Chaos (DPS + AoE)
- Un joueur maintient les tourelles (Energize, Infuse), l'autre défend
- Partage de modules entre joueurs, blueprints sauvegardables

---

## Mode de jeu

- **Endless** sur une seule map
- Vagues d'ennemis de difficulté croissante
- La progression = les modules acquis pendant la run et leur agencement
- À la mort → reset

## Décisions prises

- Les ennemis ne peuvent pas endommager/désactiver les modules d'une tourelle
- Pas d'IA adaptative
- L'éditeur de nodes est accessible tout le temps
- Pas de limite stricte de tourelles, limitée par le coût en ressources
- Pas de roguelite / pas de meta-progression
- `On Trigger` à la Q-UP pour les cascades inter-tourelles

## Questions ouvertes

- Comment le joueur obtient les modules ? (drop sur les ennemis, achat avec ressources, les deux ?)
- Coût des tourelles : fixe par châssis ? Scaling ?
- Limite de cooldown / activation stock pour éviter les boucles infinies de Signal/Repeat ?
- Limite de modules par rack d'effets, ou illimité ?
