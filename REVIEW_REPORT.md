# Iteration 2/ — 2 CRITIQUE, 1 MOYEN, 1 MINEUR — Fichiers : 50/50 — 2026-03-28

## Problèmes

| Fichier | Ligne | Sévérité | Description | Confiance | Statut |
|---------|-------|----------|-------------|-----------|--------|
| Combat/Projectile.cs | 15,39 | CRITIQUE | `_serverSide` vestige Netcode — renommé en `_dealsDamage`, callers mis à jour | 5/5 | CORRIGÉ |
| Modules/ChainBuilder.cs | 86-104 | CRITIQUE | Méthode `ExecuteZoneChain()` dead code — jamais appelée (remplacée par `ExecuteZoneChainWithRotation` dans TowerExecutor) | 5/5 | CORRIGÉ |
| Enemies/EnemyHealth.cs | 25 | MINEUR | Event `_currentHPChanged` utilise un préfixe underscore non conventionnel pour un event public | 4/5 | SIGNALÉ |
| Enemies/EnemyHealth.cs | 32-42 | MOYEN | `SetInitialHP()` et `SetMaxHP()` sont identiques (même corps). L'un est probablement superflu ou devrait avoir un comportement différent | 4/5 | SIGNALÉ |

## Nettoyage Netcode (via MCP)

| Élément | Action | Statut |
|---------|--------|--------|
| `Assets/DefaultNetworkPrefabs.asset` | Supprimé via MCP | FAIT |
| `Assets/Scenes/3CTestScene.unity` | Supprimé via MCP (pas dans build, rempli de NetworkManager/UnityTransport) | FAIT |
| GameScene — 7 missing scripts (NetworkObject components) | Auto-réparé via `manage_scene validate auto_repair` | FAIT |
| GameScene — "UI Lobby" GameObject | Supprimé via MCP (vestige lobby multijoueur) | FAIT |
| GameScene — "UI Crosshair" GameObject | Supprimé via MCP (vestige FPS) | FAIT |
| `Projectile._serverSide` → `_dealsDamage` | Renommé dans Projectile.cs + ProjectileEffect.cs | FAIT |
| Packages manifest.json | Déjà propre — aucun package Netcode/Relay/Lobby | VÉRIFIÉ |
| Scripts C# | Aucune référence Netcode restante (grep complet) | VÉRIFIÉ |
| Prefabs (*.prefab) | Aucune référence Netcode restante | VÉRIFIÉ |
| Assets (*.asset) | Aucune référence Netcode restante | VÉRIFIÉ |

## Nettoyage FPS remnants (Iteration 2)

| Élément | Action | Statut |
|---------|--------|--------|
| `Assets/Data/PlayerWeaponChassis.asset` | Supprimé via MCP (FPS weapon chassis) | FAIT |
| `Assets/UI/Crosshair/` (uss, uxml, PanelSettings) | Supprimé via MCP (FPS crosshair UI) | FAIT |
| `Assets/Scripts/Network/` | Dossier vide supprimé | FAIT |
| `Assets/Scripts/UI/Lobby/` | Dossier vide supprimé | FAIT |
| `Assets/Prefabs/Network/` → `Assets/Prefabs/Player/` | PlayerPrefab déplacé, dossier Network supprimé | FAIT |
| `ChassisDefinition.isPlayerChassis` | Champ FPS inutilisé supprimé | FAIT |
| `Controls.inputactions` — 9 actions FPS mortes | Look, Attack, AltAttack, Crouch, Jump, Previous, Next, OpenWeaponEditor, ReadyUp + bindings supprimés | FAIT |
| `PlayerSpawnMarker.cs` + prefab | Supprimés (multiplayer spawn, jamais référencé) | FAIT |
| `com.unity.cinemachine` | Retiré de manifest.json (jamais utilisé) | FAIT |
| `com.unity.timeline` | Retiré de manifest.json (jamais utilisé) | FAIT |
| `com.unity.visualscripting` | Retiré de manifest.json (jamais utilisé) | FAIT |
| Orphaned .meta files | Aucun trouvé (find scan) | VÉRIFIÉ |

## Fichiers analysés

| Module | Fichiers | Risque | Statut |
|--------|----------|--------|--------|
| Core | SimpleGameBootstrap, GameManager, GameState, WaveManager, ObjectiveController, ServiceLocator, GameStats | 1 | FAIT |
| Player | PlayerController, PlayerInteraction, PlayerInputProvider, PlayerInventory, DefaultLoadout, InteractionHUD | 1 | FAIT |
| Towers | TowerExecutor, TowerChassis, TowerInteractable, ChassisDefinition | 1 | FAIT |
| Enemies | EnemyController, EnemyHealth, EnemySpawner, EnemyAI, EnemyDefinition, EnemyAnimationController, EnemyHitFeedback, DamageTextFloat, WaveDefinition | 1 | FAIT |
| AI | EnemyAI, ThreatSource, ThreatCalculator, ThreatCalculatorConfig, EnemyAIState, EnemyTargetRegistry | 1 | FAIT |
| Combat | Projectile, StatusEffectManager, DamageInfo | 1 | FAIT |
| Modules | ChainBuilder, ModuleFactory, ModuleRegistry, ModuleDefinition, EffectInstance, DotEffect, SlowEffect, HitscanEffect, ProjectileEffect, all Triggers/Zones/Effects | 1 | FAIT |
| NodeEditor | NodeEditorController, NodeEditorScreen, NodeEditorCanvas, GraphSerializer, NodeGraphData | 2 | FAIT |
| NodeEditor UI | DesignConstants, ModuleElement, ModulePalette, ModulePaletteItem, NodeWidget, PortWidget, PopupPanel, CableRenderer, ConnectionLayer, MinimapWidget, DocumentationContent | 3 | FAIT |
| HUD | GameHUD, GameOverScreen, HUDWaveInfo, HUDPlayerStatus, HUDEventLog, HUDSystemInfo | 3 | FAIT |
| UI | WaveStartUI, InteractionPromptUI, GridBackground | 3 | FAIT |
| LevelDesign | LDtkLevelLinker, LDtkLevelPostprocessor, TerrainMapping, markers (6 files) | 2 | FAIT |
| Camera | TopDownCamera, CameraSetup | 2 | FAIT |
| Loot | ModulePickup, LootEntry, LootRarity, LootTable | 2 | FAIT |
| RadialMenu | AddModulePopup, RadialMenuCanvas, RadialMenuScreen, RadialSegment, RadialMenuController | 3 | FAIT |
| Editor | VectorStyleApplier | 6 | FAIT |
| Visual | PathVisualizer | 7 | FAIT |
| Controls.cs | Auto-généré — ignoré | 7 | FAIT |

## Patterns détectés

- Architecture propre : séparation nette Core / Player / Enemies / Modules / UI
- ServiceLocator + Singleton pattern cohérent (GameManager, GameStats, ObjectiveController)
- Null-conditional `?.` utilisé correctement partout pour les singletons
- Coroutines `WaitForX()` pattern pour dépendances d'init (PlayerController, WaveStartUI)
- Event subscribe/unsubscribe propre dans Start/OnDestroy

## Hors scope

- `Assets/Plugins/` — code tiers (Odin, Wireframe Shader, vTabs)
- `Controls.cs` — auto-généré par Input System
- `.meta`, `.shader`, `.uss`, `.uxml` — non-scripts

## Console Unity

- 0 erreurs de compilation
- NullRef dans plugins tiers (vFolders) — non lié à nos changements
- Warnings : uniquement dans plugins tiers (GetInstanceID deprecated dans WireframeShader.cs et VTabs.cs)

## Note

- `Assets/Prefabs/Player/PlayerPrefab.prefab` existe mais n'est référencé nulle part (ni dans la scène, ni dans SimpleGameBootstrap). SimpleGameBootstrap n'est attaché à aucun GameObject dans GameScene. Le player n'est pas instancié actuellement.

## Test runner

- `com.unity.test-framework` 1.6.0 présent dans manifest.json
- Pas de tests unitaires trouvés dans le projet
