# TODO — suites de la code review

Points relevés par la review de septembre 2026 mais non corrigés. Les numéros de ligne peuvent avoir bougé.

## À vérifier à la main

Ces correctifs sont déjà commités mais n'ont pas été testés à la souris :

- [ ] Échap ferme le panneau de mods, ou relance la partie au game over, sans quitter le jeu.
- [ ] Glisser un mod hors d'un slot ne retire que ce mod.
- [ ] Cliquer sur START WAVE ne pose pas de tour sous le bouton.
- [ ] Un clic droit sur un slot retire le mod, un clic droit hors du panneau le ferme.
- [ ] Redimensionner puis minimiser la fenêtre ne provoque ni crash ni écran noir.
- [ ] Au restart, les cases des anciennes tours ne restent pas rouges.

## Choix de game design à trancher

À comparer avec le projet Unity de référence avant de corriger.

1. **Burn, Frost et Wide empilés n'apportent rien de plus.** Le compilateur ajoute N stages identiques, mais `StatusEffectManager.ApplyEffect` se contente de rafraîchir un effet identique, et le premier `WideStage` marque tous les ennemis touchés. Il faudrait plutôt un seul stage dont la puissance dépend du nombre de mods (`PipelineCompiler.cs`, `StatusEffectManager.cs`, `WideStage.cs`).
2. **Pierce et Bounce consomment chacun une charge au même impact.** Les deux stages tournent en PostHit. Faut-il que Pierce soit épuisé avant que Bounce commence (`if (!ctx.Consumed) return;` dans `BounceStage`) ?
3. **Les synergies sont appliquées à toute la tour.** Elles sont détectées par groupe de mods (entre deux événements), puis fusionnées en une seule liste. Par exemple `[Burn, OnKill, Swift, Swift]` active Machinegun sur toute la tour. Il faudrait des synergies propres à chaque sous-pipeline.
4. **Napalm et Avalanche s'affichent dans l'UI mais n'ont aucun effet.** Il faut les implémenter ou les retirer de `SynergyDef.cs`.
5. **Les synergies ne sont pas détectées pareil selon l'endroit.** Le compilateur regarde toutes les paires d'un groupe, alors que les connecteurs entre slots de l'UI ne regardent que les paires adjacentes. Il faut choisir une seule règle.
6. **OnEnd ne se déclenche qu'à l'expiration.** Un projectile consommé par un impact ne lance jamais OnEnd (`ModProjectile.ProcessHit`).
7. **Les conditions sont évaluées trop tard.** IfBurning et IfFrozen sont toujours vraies quand Burn ou Frost est dans le même groupe, et IfLow se déclenche à chaque kill. Il faudrait prendre un instantané de l'état avant d'appliquer les dégâts.
8. **Split.** Les enfants perdent les tags du parent (ils changent donc de couleur), et avec Homing ils visent tous la même cible.
9. **Il n'y a pas de victoire.** Après la dernière vague, le jeu repart sur la vague 1 (`WaveManager.cs`, `_currentWave % _waves.Count`) et le HUD affiche « 6/5 ».
10. **Les JSON de `resources/data/` ne sont jamais chargés.** Les fichiers `levels/`, `enemies/` et `waves/` ne correspondent pas au schéma du code : vagues et grille sont codées en dur (`CreateTestWaves`, `GridDefinition.CreateTestGrid`). Il faut soit écrire un loader, soit supprimer ces fichiers.
11. **On peut placer une tour sur n'importe quelle case vide.** `TowerPlacement.CanPlaceAt` accepte les cases `Empty` en plus des cases `TowerSlot`.
12. **OnHit, OnKill et OnOverkill donnent comme cible à leurs enfants l'ennemi qui vient d'être touché.** Cet ennemi est déjà exclu de leurs impacts. Faut-il passer `null` pour que ces enfants cherchent une nouvelle cible ?
13. **Une tour sans mod ne tire pas** (`ModSlotExecutor.Update`). Est-ce voulu ?

## Bugs restants

- **Bloom et alpha.** `threshold.fs` soustrait aussi le seuil de l'alpha, et `final_composite.fs` ne force l'alpha à 1 que lorsque l'aberration chromatique est active. Résultat : un bloom affaibli et un sursaut de luminosité à chaque kill. Le corriger changera le rendu visuel.
- **Glow additif.** Les passes de glow additif écrivent dans le depth buffer, donc les sphères d'impact masquent les particules. Il faut encadrer ces passes avec `Rlgl.DisableDepthMask()` et `EnableDepthMask()`.
- **Sons qui se coupent.** raylib redémarre un `Sound` déjà en cours de lecture, donc `MaxInstances` compte des voix qui n'existent pas. Il faut utiliser `LoadSoundAlias` pour avoir plusieurs copies par variante.
- **Effets d'impact dans le vide.** `OnProjectileDestroyed` déclenche flash, particules, son et shake même quand le projectile expire en l'air, et les double lors d'un impact final.
- **Mode `--screenshot`.** L'image est enregistrée dans `bin/` et non dans le dossier de lancement, et elle manque peut-être le HUD (le batch de dessin n'est pas vidé avant `TakeScreenshot`).
- **Fallback de shader inopérant.** `LoadShader` renvoie le shader par défaut en cas d'échec, donc `Id > 0` est toujours vrai. Il faut comparer avec `Rlgl.GetShaderIdDefault()`.
- **`TOTAL_UPTIME`** sur l'écran de game over compte à partir du game over, et non depuis le début de la partie.
- **Leech** soigne sur la base des dégâts bruts, même en cas d'overkill.
- **Overkill.** `OverkillAmount` ignore le multiplicateur de vulnérabilité. C'est latent tant qu'aucun stage n'applique Vulnerability.
- **Trails.** Leur nombre est plafonné à 256 : au-delà, les projectiles issus de Split n'en ont pas.
- **Glow de la grille.** Une lueur résiduelle reste affichée quand la simulation de déformation s'endort.
- **Sons chargés mais jamais joués** : EnemyDeath, ObjectiveHit, LootDrop, LootCollect, TowerPlaceInvalid et tous les sons de l'éditeur de mods.

## Perf

- **HUD et `ModSlotPanel`.** Des chaînes sont allouées à chaque frame (interpolations, labels ImGui). Il faut les mettre en cache et ne les reconstruire que quand la valeur change.
- **`DamageTextSystem`.** Il fait un `ToString()` à chaque impact : mettre en cache les chaînes de 0 à 999.
- **Bounce et Homing** parcourent encore tous les ennemis. Il leur faut une portée maximale pour pouvoir interroger le SpatialHash.
- **`ImpactFlash`.** Trois `DrawSphere` 16×16 par flash, c'est trop de géométrie. Passer à des sphères basse résolution ou à des billboards.
- **Particules et voxels.** Jusqu'à environ 10 000 `DrawCube` par frame. Il faut les regrouper en triangles Rlgl ou les instancier.
- **Pools pleins (`VoxelPool`, `ParticleEmitter`).** Pour recycler l'entrée la plus ancienne, ils parcourent tout le pool. Utiliser un curseur circulaire.
- **`GridWarpManager.DropStone` et `Shockwave`** parcourent les ~3 100 sommets de la grille. Il faut se limiter au rayon concerné.
- **`GridVisual`.** Le mesh est réenvoyé au GPU à chaque frame rendue. Il ne faudrait le faire que lorsqu'un pas de simulation l'a modifié.
- **Pickups.** Le mode de blending est changé pour chaque pickup, ce qui vide deux fois le batch de dessin par pickup.
- **`Profiler`.** Il alloue un dictionnaire par frame et garde un historique sans limite.
- **Enemy registry.** `EnemyRegistry.Unregister` fait une recherche linéaire, et la liste des ennemis est dupliquée entre le registre et `EnemySpawner._activeEnemies`.
- **Synergies.** Ce sont des `List<SynergyEffect>` copiées à chaque tir. Un bitmask `[Flags]` éviterait ces copies.

## Refactor

- **Découper `ModSlotPanel.cs`** (~740 lignes) : panneau d'inventaire, chaîne de slots, zone d'info et style. `GetModColor`/`GetModDescription` iraient en extensions de `ModType`, et `TV4`/`TV4A` dans `DesignTokens`.
- **Créer une classe de base `EventStage`** pour les 7 stages d'événements quasi identiques.
- **Extraire shake, bloom et chromatic** de `GameLoop.cs` dans une classe `ScreenFx`.
- **Fusionner les deux branches `Render3D`** de `RunFrame`.
- **Nettoyer les ressources inutilisées** : les shaders `chromatic`, `vignette`, `pixelgrid`, `neonprojectile`, `neontrail`, `vectorglow`, `instancing`, `cybergrid_debug` et `vectoroutline`, ainsi que `resources/data/audio_manifest.json`.
- **Code mort** :
  - `GameStats.WaveReached`
  - `EnemyPool.Clear`
  - `ModTypeExtensions.IsConditional`
  - `SynergyEffect.Barrage`
  - `GridManager.WorldToGrid`
  - `WaveManager._waveClearedDuration`
  - les tests `!= null` toujours faux sur `HitInstances` et `Synergies`
- **Accès à l'objectif.** On y accède à la fois par `ObjectiveController.Instance` et par `ServiceLocator` : n'en garder qu'un.
