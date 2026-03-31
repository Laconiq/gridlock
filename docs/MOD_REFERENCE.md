# Référence des Mods & Synergies

## Phases du Pipeline

Chaque projectile exécute ses mods dans 5 phases, dans l'ordre :

| Phase | Quand | Exemples |
|-------|-------|----------|
| **Configure** | Au spawn, définit les stats de base | Heavy, Swift, Split |
| **OnUpdate** | Chaque frame tant que le projectile vit | Homing, OnPulse, OnDelay |
| **OnHit** | À l'impact sur un ennemi | Burn, Frost, Shock, Wide, Void, Leech |
| **PostHit** | Après résolution des dégâts | Pierce, Bounce, OnKill, OnOverkill |
| **OnExpire** | Quand la durée de vie expire | OnEnd |

---

## Mods Trait

### Comportement

| Mod | Phase | Effet | Cumulable |
|-----|-------|-------|-----------|
| **Homing** | OnUpdate | Tourne progressivement vers la cible (8 u/s) | Non |
| **Pierce** | PostHit | Traverse les ennemis (3 perforations) | Non |
| **Bounce** | PostHit | Rebondit vers l'ennemi non-touché le plus proche (3 rebonds) | Non |
| **Split** | Configure | Éclate en N projectiles sur un arc de 90°. N = 1 + nombre de slots | Nombre uniquement |
| **Heavy** | Configure | Dégâts ×1.5, Vitesse ×0.7, Taille ×1.3 | Oui (×N) |
| **Swift** | Configure | Dégâts ×0.75, Vitesse ×1.5 | Oui (×N) |
| **Wide** | OnHit | Dégâts de zone dans un rayon de 2u autour de l'impact | Oui |

### Élémentaire

| Mod | Phase | Effet | Cumulable |
|-----|-------|-------|-----------|
| **Burn** | OnHit | DoT : 3 dmg/tick, 3s durée, 0.5s intervalle | Oui |
| **Frost** | OnHit | Ralentissement : 65%, 2s durée | Oui |
| **Shock** | OnHit | Éclair en chaîne : 1 chaîne, 3u rayon | Oui |
| **Void** | OnHit | Dégâts = 5% des PV actuels de la cible (ignore les dégâts de base) | Oui |
| **Leech** | OnHit | Soigne l'objectif : 12% des dégâts infligés | Oui |

---

## Mods Événement

Les événements coupent la chaîne de slots : les mods avant = projectile principal, les mods après = sous-projectile.

### Événements de base

| Événement | Phase | Déclencheur | Dégâts du sous-projectile |
|-----------|-------|-------------|---------------------------|
| **OnHit** | OnHit | Chaque impact | ×0.6 |
| **OnKill** | PostHit | La cible meurt de ce tir | ×0.6 |
| **OnEnd** | OnExpire | Le projectile expire | ×0.6 |

### Événements dépendants d'un mod

| Événement | Phase | Requiert | Déclencheur | Dégâts du sous-projectile |
|-----------|-------|----------|-------------|---------------------------|
| **OnPierce** | PostHit | Pierce | Chaque traversée | ×0.6 |
| **OnBounce** | PostHit | Bounce | Chaque rebond | ×0.6 |

### Événements temporels

| Événement | Phase | Déclencheur | Dégâts du sous-projectile |
|-----------|-------|-------------|---------------------------|
| **OnDelay** | OnUpdate | 0.5s après le spawn, puis consomme le projectile | ×0.6 |
| **OnPulse** | OnUpdate | Toutes les 0.3s tant que le projectile vit | ×0.3 |

### Événements conditionnels

| Événement | Phase | Condition | Dégâts du sous-projectile |
|-----------|-------|-----------|---------------------------|
| **IfBurning** | OnHit | La cible a un DoT actif | ×0.6 |
| **IfFrozen** | OnHit | La cible a un ralentissement actif | ×0.6 |
| **IfShocked** | OnHit | Le contexte a le tag Shock | ×0.6 |
| **IfLow** | OnHit | PV de la cible < 30% max | ×0.6 |

### Événements d'overkill

| Événement | Phase | Condition | Dégâts du sous-projectile |
|-----------|-------|-----------|---------------------------|
| **OnCrit** | PostHit | Dégâts > PV restants de la cible | ×0.8 |
| **OnOverkill** | PostHit | Dégâts > PV restants de la cible | ×1.0 |

---

## Synergies

Les synergies s'activent quand les deux mods déclencheurs sont présents dans la même liste de slots (avant toute frontière d'événement). Les bonus de paires adjacentes comptent aussi pour le cumul.

| Synergie | Déclencheur | Effet | Statut |
|----------|-------------|-------|--------|
| **Railgun** | Heavy + Heavy | Pierce gratuit avec +2 perforations | Implémenté |
| **Machinegun** | Swift + Swift | Double cadence de tir | Niveau tour |
| **Blizzard** | Frost + Frost | +0.5s stun (0% mouvement) en plus du ralentissement | Implémenté |
| **Tesla** | Shock + Shock | Chaîne vers 3 ennemis au lieu de 1 | Implémenté |
| **Missile** | Homing + Swift | Verrouillage instantané (pas de lerp) | Implémenté |
| **Meteor** | Heavy + Wide | Rayon de zone doublé (2u → 4u) | Implémenté |
| **Barrage** | Split + Split | Bonus ×5 au lieu de ×3+×3 | Implémenté |
| **Vampire** | Leech + Heavy | Taux de drain 12% → 40% | Implémenté |
| **Siphon** | Shock + Void | Les dégâts Void soignent l'objectif | Implémenté |
| **Napalm** | Burn + Wide | Zone de feu persistante | Pas encore |
| **Avalanche** | Frost + Wide | Zone de ralentissement persistante | Pas encore |
| **Ricochet** | Pierce + Bounce | Rebonds illimités | Pas encore |
| **ThermalShock** | Burn + Frost | Explosion à 50% des PV | Pas encore |

---

## Exemples de chaînes de slots

Notation : `[Trait Trait ...] Événement [Trait ...]`

| Build | Comportement |
|-------|-------------|
| `[Heavy Heavy Pierce]` | Synergie Railgun : projectile lent, puissant, perforant avec +2 perforations |
| `[Swift Swift Homing]` | Synergies Machinegun + Missile : tir rapide, verrouillage instantané |
| `[Heavy Wide]` | Synergie Meteor : projectile lent avec zone de 4u |
| `[Burn Frost]` | Synergie ThermalShock (pas encore implémenté) : explosion à 50% PV |
| `[Pierce] OnPierce [Frost]` | Chaque traversée engendre un sous-projectile givré |
| `[Heavy] OnKill [Split Burn]` | Au kill, engendre des sous-projectiles éclatés enflammés |
| `[Shock Shock] OnHit [Void]` | Chaîne Tesla + chaque impact engendre un sous-projectile Void |
| `[Swift Homing] OnPulse [Frost]` | Missile qui pulse périodiquement des sous-projectiles givrés |
| `[Heavy Pierce] OnOverkill [Wide Burn]` | L'overkill engendre des explosions de zone enflammées à plein dégâts |

---

## Règles de cumul

- **Traits cumulables** (Heavy, Swift, Wide, Burn, Frost, Shock, Void, Leech) : chaque instance applique son multiplicateur/effet à nouveau. Les paires adjacentes donnent +1 stack bonus.
- **Traits non-cumulables** (Homing, Pierce, Bounce, Split) : s'appliquent une seule fois quel que soit le nombre. Split utilise le nombre pour le compte de projectiles.
- **Bonus de paire adjacente** : deux mods identiques consécutifs dans la liste de slots donnent un stack supplémentaire (ex. `[Heavy Heavy]` = 2 instances + 1 paire = 3× Heavy).

## Tags (Bitfield)

Utilisés pour les vérifications rapides de présence de mod dans les conditions et synergies.

```
Homing = 1 << 0    Pierce = 1 << 1    Bounce = 1 << 2
Split  = 1 << 3    Heavy  = 1 << 4    Swift  = 1 << 5
Wide   = 1 << 6    Burn   = 1 << 7    Frost  = 1 << 8
Shock  = 1 << 9    Void   = 1 << 10   Leech  = 1 << 11
```
