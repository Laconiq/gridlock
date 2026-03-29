# Mod Slot Presets

Presets de test a appliquer directement sur les tours en scene. Chaque preset montre un archetype different du systeme de mod slots.

Notation : `[ Mod ]` = trait, `[ ⟐ Event ]` = event. Lecture gauche → droite.

---

## Tier 1 — 3 slots (builds simples)

### P01 — Basic Shooter
```
[ Homing ] [ ___ ] [ ___ ]
```
Balle guidee basique. Le strict minimum. Doit deja marcher mieux qu'un tir droit car le homing compense le positionnement.

### P02 — Sniper
```
[ Heavy ] [ Heavy ] [ Pierce ]
```
Balle massive, lente, qui traverse tout. Synergy **Railgun** (Heavy+Heavy = Pierce gratuit, donc double pierce). Ideal fond de lane.

### P03 — Sprayer
```
[ Swift ] [ Swift ] [ Burn ]
```
Rafale rapide, faible degats, mais DOT feu. Synergy **Machinegun** (Swift+Swift = fire rate doublee). Crowd control par attrition.

### P04 — Frost Tower
```
[ Frost ] [ Frost ] [ Wide ]
```
Slow en zone. Synergy **Blizzard** (Frost+Frost = freeze stun). Synergy **Avalanche** (Frost+Wide = zone slow persistante). Setup pour autres tours.

### P05 — Void Sniper
```
[ Void ] [ Heavy ] [ ___ ]
```
Anti-tank. Le Void scale avec les HP de la cible, Heavy ajoute du burst. Bon contre les gros ennemis lents.

### P06 — Healer
```
[ Leech ] [ Heavy ] [ ___ ]
```
Soigne l'objectif en faisant des degats. Synergy **Vampire** (Leech+Heavy = 40% drain). Tour defensive.

---

## Tier 2 — 5 slots (builds avec 1 event)

### P07 — Cluster Bomb
```
[ Heavy ] [ ⟐ Hit ] [ Split ] [ Wide ] [ Burn ]
```
Balle lourde. A l'impact → 3 eclats AOE enflammes. Le bread & butter du mid-game. Bon contre les groupes.

### P08 — Missile Battery
```
[ Split ] [ Homing ] [ Swift ] [ ⟐ Hit ] [ Shock ]
```
3 missiles rapides guides. Synergy **Missile** (Homing+Swift = snap direct). A l'impact de chacun → electrocute un voisin. Spread + single target hybrid.

### P09 — Chain Lightning
```
[ Shock ] [ Shock ] [ ⟐ Chain ] [ Burn ] [ ___ ]
```
Synergy **Tesla** (Shock+Shock = chain 3). A chaque saut → applique Burn en plus. Setup Burning + Shocked sur tout un groupe.

### P10 — Frost Mine
```
[ Frost ] [ Wide ] [ ⟐ End ] [ Split ] [ Frost ]
```
Balle lente avec slow AOE. Synergy **Avalanche** (Frost+Wide). Quand elle expire → 3 eclats glacants. Double couche de crowd control.

### P11 — Piercing Beam
```
[ Pierce ] [ Heavy ] [ Burn ] [ ⟐ Pierce ] [ Shock ]
```
Traverse les ennemis en les brulant. A CHAQUE traversee → electrocute un voisin. Event **⟐ Pierce** dependant du mod Pierce. Plus y'a d'ennemis en ligne, plus c'est puissant.

### P12 — Bouncing Orb
```
[ Bounce ] [ Frost ] [ ⟐ Bounce ] [ Wide ] [ ___ ]
```
Rebondit entre ennemis en gelant. A chaque rebond → mini AOE de slow. Event **⟐ Bounce** dependant du mod Bounce.

### P13 — Delayed Nuke
```
[ Swift ] [ ⟐ Delay ] [ Wide ] [ Wide ] [ Burn ]
```
Balle rapide. Apres 0.5s de vol (en plein dans la horde) → mega explosion de feu. Le placement de la tour determine OU la bombe explose.

### P14 — Living Frost Orb
```
[ Homing ] [ ⟐ Pulse ] [ Frost ] [ Wide ] [ ___ ]
```
Orbe guidee qui emet un pulse glacant AOE periodiquement pendant qu'elle vole. Crowd control mobile continu.

### P15 — Executioner
```
[ Heavy ] [ Void ] [ ⟐ If-Low ] [ Wide ] [ Burn ]
```
Gros degats + % HP. Si la cible passe sous 30% → explosion de feu AOE. Event conditionnel **⟐ If-Low**. Finisher naturel.

### P16 — Combo Exploiter
```
[ Heavy ] [ ⟐ If-Frozen ] [ Split ] [ Burn ] [ ___ ]
```
Cible les geles (setup par une autre tour Frost). Si la cible est Frozen → 3 eclats de feu. **Synergie inter-tours** : Tour A freeze, Tour B capitalise.

### P17 — Status Detonator
```
[ Shock ] [ ⟐ If-Burning ] [ Wide ] [ Heavy ] [ ___ ]
```
Electrocute, et si la cible brule deja (setup par autre tour) → grosse explosion. Combo inter-tours Burn + Shock.

---

## Tier 3 — 7 slots (builds avec 2+ events)

### P18 — Cascade Nuke
```
[ Homing ] [ ⟐ Hit ] [ Split ] [ ⟐ Kill ] [ Wide ] [ Burn ] [ Heavy ]
```
Balle homing → impact split en 3 → si un eclat tue → mega explosion feu. 3 etages de cascade.

### P19 — Fractal Bomb
```
[ Heavy ] [ ⟐ Delay ] [ Split ] [ ⟐ Delay ] [ Split ] [ Wide ] [ ___ ]
```
Balle → 0.5s → 3 balles → 0.5s → 9 balles → 9 explosions AOE. Cluster bomb fractale a retardement.

### P20 — Tesla Coil
```
[ Shock ] [ Shock ] [ ⟐ Chain ] [ Frost ] [ ⟐ If-Frozen ] [ Wide ] [ Burn ]
```
Tesla chain (3 sauts). Chaque saut freeze. Si freeze → explosion de feu AOE. Auto-combo : la tour se setup et capitalise elle-meme.

### P21 — Orbital Artillery
```
[ Pierce ] [ Burn ] [ ⟐ Pierce ] [ Split ] [ Homing ] [ ⟐ Hit ] [ Wide ]
```
Traverse en brulant. A chaque pierce → 3 sous-missiles homings. Chaque sous-missile explose en AOE. Volume de feu enorme.

### P22 — Void Harvester
```
[ Void ] [ Leech ] [ ⟐ Kill ] [ Split ] [ Void ] [ Leech ] [ Wide ]
```
Drain + heal. Si kill → 3 eclats drain AOE. Tour auto-sustain extreme. Le Void scale sur les gros, les eclats nettoient les petits.

### P23 — Pulse Engine
```
[ Homing ] [ Swift ] [ ⟐ Pulse ] [ Frost ] [ ⟐ Pulse ] [ Shock ] [ ___ ]
```
Missile rapide avec 2 auras pulsantes : frost + shock. Crowd control mobile total. Gele et electrocute tout sur son passage.

### P24 — Overkill Cascade
```
[ Heavy ] [ Heavy ] [ ⟐ Overkill ] [ Split ] [ Heavy ] [ ⟐ Hit ] [ Wide ]
```
Railgun (Heavy+Heavy). Si overkill → 3 eclats lourds. Chaque eclat explose en AOE. One-shot → chain reaction. Event rare **⟐ Overkill**.

### P25 — Crit Chain
```
[ Heavy ] [ Shock ] [ ⟐ Crit ] [ Split ] [ Burn ] [ ⟐ Chain ] [ Wide ]
```
Sur crit (10%) → 3 eclats de feu. Chaque chaine electrique → explosion AOE. Evenement rare **⟐ Crit** + chaine dependante. High variance, high reward.

---

## Builds inter-tours (a tester ensemble)

### Duo — Freeze & Shatter

| Tour | Build |
|------|-------|
| **Tour A** (setup) | `[ Frost ] [ Frost ] [ Pierce ]` — Blizzard perforante, freeze tout |
| **Tour B** (payoff) | `[ Heavy ] [ ⟐ If-Frozen ] [ Wide ] [ Burn ] [ ___ ]` — Explose les geles |

### Duo — Burn & Detonate

| Tour | Build |
|------|-------|
| **Tour A** (setup) | `[ Split ] [ Swift ] [ Burn ]` — Spray incendiaire |
| **Tour B** (payoff) | `[ Shock ] [ ⟐ If-Burning ] [ Wide ] [ Heavy ] [ ___ ]` — Detone les brulants |

### Trio — Full Element Combo

| Tour | Build |
|------|-------|
| **Tour A** (frost) | `[ Frost ] [ Frost ] [ Wide ]` — Blizzard + Avalanche |
| **Tour B** (burn) | `[ Burn ] [ ⟐ If-Frozen ] [ Wide ] [ ___ ] [ ___ ]` — Thermal Shock les geles |
| **Tour C** (execute) | `[ Heavy ] [ Void ] [ ⟐ If-Low ] [ Split ] [ Shock ]` — Finit les survivants |

### Duo — Heal Battery

| Tour | Build |
|------|-------|
| **Tour A** (damage) | `[ Heavy ] [ Pierce ] [ Burn ]` — DPS brut |
| **Tour B** (sustain) | `[ Leech ] [ Heavy ] [ Bounce ] [ ⟐ Bounce ] [ Leech ]` — Vampire bouncing, heal a chaque rebond |

---

## Matrice des presets par archetype

| Archetype | Presets | Description |
|-----------|---------|-------------|
| **DPS pur** | P02, P05, P07, P18, P24 | Maximise les degats bruts |
| **Crowd control** | P04, P09, P10, P12, P14, P23 | Slow/freeze/stun pour tout le groupe |
| **Anti-tank** | P02, P05, P15, P22 | Cible les gros ennemis (Void, Heavy) |
| **Combo inter-tours** | P16, P17, Duos/Trio | Necessite une tour de setup + payoff |
| **Auto-combo** | P09, P11, P20, P25 | La tour se setup et capitalise seule |
| **Sustain** | P06, P22, Duo Heal | Soigne l'objectif via Leech |
| **AOE** | P07, P13, P19, P21 | Volume de feu, nettoie les groupes |
| **High variance** | P24, P25 | Crit/Overkill — parfois broken, parfois rien |
