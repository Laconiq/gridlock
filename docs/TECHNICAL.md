# AIWE — Document Technique

## Vue d'ensemble

AIWE est un FPS Tower Defense coopératif en ligne (2-4 joueurs) développé sous Unity 6 (6000.3.10f1) avec le Universal Render Pipeline (URP). Les joueurs défendent une zone contre des vagues d'ennemis en combinant combat FPS et défenses modulaires configurables via un éditeur de nœuds visuel.

## Stack technique

| Composant | Technologie | Version |
|-----------|-------------|---------|
| Moteur | Unity 6 | 6000.3.10f1 |
| Rendu | URP (Universal Render Pipeline) | 17.3.0 |
| Input | New Input System | 1.18.0 |
| Réseau | Netcode for GameObjects (NGO) | 2.10.0 |
| Services multi | Unity Multiplayer Services | 2.1.3 |
| Navigation IA | AI Navigation | 2.0.12 |
| Level Design | LDtk + LDtkToUnity | 6.12.1 |
| Cinématiques | Timeline | 1.8.10 |
| Scripting visuel | Visual Scripting | 1.9.9 |
| Tests | Test Framework | 1.6.0 |
| Backend scripting | IL2CPP | — |

## Plugins tiers

| Plugin | Usage |
|--------|-------|
| **Odin Inspector** (Sirenix) | Inspectors custom avancés, sérialisation |
| **Feel** (MoreMountains) | Feedbacks, juice, haptics (NiceVibrations) |
| **PlayModeSave** | Sauvegarde des modifications en Play Mode |
| **vFavorites / vFolders / vHierarchy / vInspector / vTabs** | Outils éditeur (productivité) |

## Architecture du projet

### Arborescence Assets

```
Assets/
├── Animations/          # Clips d'animation, Animator Controllers
├── LDtk/                # Fichiers LDtk (niveaux, tilesets)
│   ├── World.ldtk       # Projet LDtk principal
│   └── Tilesets/         # Images PNG des tilesets
├── Materials/           # Matériaux URP
├── Meshes/              # Modèles 3D (.fbx, .obj)
├── Prefabs/             # Prefabs réutilisables
│   ├── Chassis/         # Prefabs des types de tourelles
│   ├── Modules/         # Prefabs des modules (Trigger, Zone, Effect)
│   ├── Enemies/         # Prefabs ennemis
│   └── Entities/        # Entités LDtk (spawns, waypoints, etc.)
├── Scenes/              # Scènes Unity
├── Scripts/             # Code C#
│   ├── Controls.inputactions  # Définition des bindings input
│   ├── Controls.cs            # Wrapper auto-généré (NE PAS MODIFIER)
│   ├── Core/            # Systèmes centraux (GameManager, WaveManager)
│   ├── Player/          # Contrôleur FPS, armes, interactions
│   ├── Towers/          # Système de chassis + modules
│   ├── NodeEditor/      # Éditeur de nœuds visuel (UI)
│   ├── Enemies/         # IA ennemis, pathfinding
│   ├── Network/         # Logique réseau (NGO)
│   └── UI/              # Interfaces utilisateur
├── Settings/            # Configs URP
│   ├── PC_RPAsset.asset
│   ├── PC_Renderer.asset
│   └── UniversalRenderPipelineGlobalSettings.asset
├── Textures/            # Textures et sprites
├── Audio/               # SFX et musique
└── Plugins/             # Plugins tiers (Odin, Feel, etc.)
```

### Architecture logicielle

```
┌─────────────────────────────────────────────────┐
│                   Game Manager                   │
│         (état de jeu, vagues, victoire)          │
├──────────┬──────────┬───────────┬───────────────┤
│  Player  │  Towers  │  Enemies  │   Network     │
│  System  │  System  │  System   │   Layer       │
├──────────┼──────────┼───────────┤   (NGO)       │
│ FPS Ctrl │ Chassis  │ AI Nav    │               │
│ Weapons  │ Modules  │ Spawner   │               │
│ Input    │ NodeGraph│ Waves     │               │
└──────────┴──────────┴───────────┴───────────────┘
         ▲                              ▲
         │         Input System         │
         └──────────────────────────────┘
         KB/Mouse · Gamepad · Touch · XR
```

## Système de modules (Node Editor)

Architecture centrale du jeu. Chaque tourelle (chassis) possède des slots de **Triggers** qui démarrent des chaînes d'exécution :

```
Trigger (QUAND) ──► Zone (OÙ/QUI) ──► Effect (FAIT QUOI)
                          │
                          ▼
                    Zone (chaîné) ──► Effect
```

### Types de modules

- **Trigger** : événement déclencheur (On Enemy Enter, On Timer, On Kill, etc.)
- **Zone** : sélection de cibles (Nearest Enemy, All In Range, Self, etc.)
- **Effect** : action concrète (Projectile, Hitscan, Slow, Knockback, etc.)

### Types de chassis

| Chassis | Position | Angle | Slots Trigger |
|---------|----------|-------|---------------|
| Sentinelle | Sol | 360° | 3 |
| Murale | Mur | — | 2 |
| Plafonnier | Plafond | — | 2 |
| Barricade | Sol (bloque) | — | 1 |
| Piédestal | Sol (portée+) | — | 2 |
| Joueur | Mobile | — | 2 (Tir primaire/secondaire) |

## Level Design avec LDtk

### Workflow

1. **Création** : Niveaux designés dans LDtk (éditeur externe)
2. **Import** : Le package `LDtkToUnity` importe automatiquement les `.ldtk` dans Unity via ScriptedImporter
3. **Mapping** :
   - Tile layers → Unity Tilemaps
   - IntGrid layers → Données de collision / terrain
   - Entity layers → Instanciation de prefabs (spawns, waypoints, emplacements de tourelles)
4. **Live reload** : Modifications dans LDtk détectées et réimportées automatiquement

### Installation LDtkToUnity

Via OpenUPM scoped registry :
1. `Edit > Project Settings > Package Manager`
2. Ajouter scoped registry : Name = `OpenUPM`, URL = `https://package.openupm.com`, Scope = `com.cammin.ldtkunity`
3. Installer depuis Package Manager > My Registries

### Entités LDtk prévues

| Entité | Rôle |
|--------|------|
| PlayerSpawn | Point d'apparition joueur |
| EnemySpawn | Point d'apparition vagues |
| TowerSlot | Emplacement autorisé pour tourelles |
| Waypoint | Point de passage IA ennemis |
| DefenseZone | Zone à défendre |

## Réseau (Netcode for GameObjects)

- **Topologie** : Client-serveur (host ou serveur dédié)
- **Services** : Unity Relay (NAT traversal) + Unity Lobby (matchmaking)
- **Synchronisation** : NetworkVariables pour l'état, RPCs pour les événements
- **Joueurs** : 2-4 en coopération

## Input System

### Action Maps

**Player** : Move, Look, Attack, Interact (hold), Crouch, Jump, Sprint, Previous, Next

**UI** : Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevicePosition, TrackedDeviceOrientation

### Control Schemes

Keyboard & Mouse, Gamepad, Touch, Joystick, XR

## Plateformes cibles

| Plateforme | Statut |
|------------|--------|
| PC (Windows/Mac/Linux) | Principale |
| Android (SDK 25+) | Secondaire |
| iOS (15.0+) | Secondaire |
| visionOS (1.0+) | Exploratoire |

## Rendu (URP)

- **Config PC** : `PC_RPAsset` — qualité desktop (shadows, SSAO, post-processing)
- **Renderer** : `PC_Renderer` — Forward rendering avec render features
- **Volume profiles** : `DefaultVolumeProfile`, `SampleSceneProfile`
