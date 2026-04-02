using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Audio;
using Gridlock.Camera;
using Gridlock.Enemies;
using Gridlock.Grid;
using Gridlock.Input;
using Gridlock.Loot;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Gridlock.Rendering;
using Gridlock.Towers;
using Gridlock.Visual;
using Gridlock.UI;
using System.Diagnostics;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace Gridlock.Core
{
    public sealed partial class GameLoop
    {
        private const float FixedDt = 1f / 60f;
        private const float MaxAccumulator = 0.25f;

        private InputManager _input = null!;
        private GameManager _gameManager = null!;
        private GameStats _gameStats = null!;
        private GridManager _gridManager = null!;
        private GridWarpManager _warpManager = null!;
        private GridVisual _gridVisual = null!;
        private ObjectiveController _objective = null!;
        private IsometricCamera _camera = null!;
        private PlayerInventory _inventory = null!;
        private TowerPlacement _towerPlacement = null!;
        private EnemySpawner _enemySpawner = null!;
        private WaveManager _waveManager = null!;
        private LootDropper _lootDropper = null!;
        private ParticleEmitter _particles = null!;
        private TrailSystem _trails = null!;
        private PathVisualizer _pathVisualizer = null!;
        private PostProcessing _postProcessing = null!;
        private SoundManager _soundManager = null!;

        private readonly List<ModProjectile> _projectiles = new();
        private readonly List<ModProjectile> _projectileRemovalBuffer = new();
        private readonly List<ModProjectile> _projectileSpawnBuffer = new();

        private readonly Dictionary<int, int> _projectileTrails = new();

        private VoxelPool _voxelPool = null!;
        private ImpactFlash _impactFlash = null!;

        private readonly Dictionary<int, float> _towerSpinAngles = new();
        private readonly Dictionary<int, float> _towerBobPhases = new();

        private Shader _outlineShader;
        private Material _outlineMaterial;
        private bool _outlineShaderLoaded;
        private int _locLineColor;
        private int _locEmissionIntensity;
        private int _locEdgeWidth;

        private HUD _hud = null!;
        private ModSlotPanel _modPanel = null!;
        private GameOverScreen _gameOverScreen = null!;
        private bool _imguiInitialized;
        private Tower? _selectedTower;
        private Tower? _hoveredTower;
        private float _accumulator;
        private bool _postProcessingAvailable;
        private float _chromaticDecay;
        private float _bloomPulse = 1.5f;
        private const float BaseBloomIntensity = 1.5f;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeIntensity;

        private readonly Stopwatch _frameSw = Stopwatch.StartNew();
        private string? _pendingScreenshot;

        public void RequestScreenshot(string path) => _pendingScreenshot = path;

        public void StartBenchmark()
        {
            var def = _gridManager.Definition;
            int placed = 0;

            for (int y = 0; y < def.Height && placed < 4; y++)
                for (int x = 0; x < def.Width && placed < 4; x++)
                {
                    var cell = _gridManager.GetRuntimeCell(x, y);
                    if (cell != CellType.Empty && cell != CellType.TowerSlot) continue;

                    bool nearPath = false;
                    for (int dy = -1; dy <= 1 && !nearPath; dy++)
                        for (int dx = -1; dx <= 1 && !nearPath; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < def.Width && ny >= 0 && ny < def.Height
                                && _gridManager.GetRuntimeCell(nx, ny) == CellType.Path)
                                nearPath = true;
                        }

                    if (!nearPath) continue;
                    var worldPos = _gridManager.GridToWorld(new Vector2Int(x, y));
                    if (_towerPlacement.TryPlace(worldPos, isOverUI: false) != null)
                        placed++;
                }

            _gameManager.SetState(GameState.Wave);
            _gameStats.SetWave(1);
        }

        public void Initialize()
        {
            _input = new InputManager();

            _gameStats = new GameStats();
            _gameStats.Init();

            _gameManager = new GameManager();
            _gameManager.Init(GameState.Preparing);

            var gridDef = GridDefinition.CreateTestGrid();
            _gridManager = new GridManager(gridDef);
            _gridManager.Init();

            _objective = new ObjectiveController(gridDef.ObjectiveHP);
            _objective.Init(_gridManager);

            _camera = new IsometricCamera();
            _camera.Init();

            _warpManager = new GridWarpManager();

            _gridVisual = new GridVisual();
            _gridVisual.Init(_gridManager, _warpManager);

            var cyberGridShader = Raylib.LoadShader(
                "resources/shaders/glsl330/cybergrid.vs",
                "resources/shaders/glsl330/cybergrid.fs");
            if (cyberGridShader.Id > 0)
            {
                _gridVisual.SetShader(cyberGridShader);
                Console.WriteLine("[GameLoop] CyberGrid shader loaded.");
            }
            else
            {
                Console.WriteLine("[GameLoop] WARNING: CyberGrid shader failed to load, using fallback grid.");
            }

            WireframeMeshes.Init();
            _outlineShader = Raylib.LoadShader(
                "resources/shaders/glsl330/vectoroutline.vs",
                "resources/shaders/glsl330/vectoroutline.fs");
            if (_outlineShader.Id > 0)
            {
                _outlineShaderLoaded = false; // Disabled — wireframe shader renders incorrectly at isometric angles
                _locLineColor = Raylib.GetShaderLocation(_outlineShader, "lineColor");
                _locEmissionIntensity = Raylib.GetShaderLocation(_outlineShader, "emissionIntensity");
                _locEdgeWidth = Raylib.GetShaderLocation(_outlineShader, "edgeWidth");

                unsafe
                {
                    int mvpLoc = Raylib.GetShaderLocation(_outlineShader, "mvp");
                    if (mvpLoc >= 0)
                        _outlineShader.Locs[(int)ShaderLocationIndex.MatrixMvp] = mvpLoc;

                    int matModelLoc = Raylib.GetShaderLocation(_outlineShader, "matModel");
                    if (matModelLoc >= 0)
                        _outlineShader.Locs[(int)ShaderLocationIndex.MatrixModel] = matModelLoc;
                }

                _outlineMaterial = Raylib.LoadMaterialDefault();
                unsafe { _outlineMaterial.Shader = _outlineShader; }

                Console.WriteLine("[GameLoop] VectorOutline shader loaded.");
            }
            else
            {
                Console.WriteLine("[GameLoop] WARNING: VectorOutline shader failed to load, using fallback wireframes.");
            }

            _inventory = new PlayerInventory();
            _inventory.Init();
            AddStarterMods();

            var defaultTowerData = DefaultTowerData();
            var defaultPreset = DefaultPreset();
            _towerPlacement = new TowerPlacement(_gridManager, defaultTowerData, defaultPreset);
            _towerPlacement.OnTowerPlaced += OnTowerPlaced;

            _enemySpawner = new EnemySpawner(_gridManager);
            _enemySpawner.OnEnemyKilled += OnEnemyKilled;
            _waveManager = new WaveManager(CreateTestWaves(), _enemySpawner);
            _waveManager.OnWaveCleared += OnWaveCleared;

            _lootDropper = new LootDropper(new LootTable { DropChance = 0.5f });

            _pathVisualizer = new PathVisualizer();
            _pathVisualizer.Init(_gridManager);

            _particles = new ParticleEmitter(1024);
            _trails = new TrailSystem();

            _voxelPool = new VoxelPool();
            _voxelPool.Init();

            _impactFlash = new ImpactFlash();
            _impactFlash.Init();

            _soundManager = new SoundManager();
            _soundManager.Init(BuildSoundConfigs());
            _soundManager.LoadFromJson("resources/data/audio_config.json");

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            _postProcessing = new PostProcessing();
            try
            {
                _postProcessing.Init(screenW, screenH);
                _postProcessingAvailable = true;
            }
            catch
            {
                _postProcessingAvailable = false;
            }

            _gameManager.OnStateChanged += OnGameStateChanged;
            _objective.OnDestroyed += OnObjectiveDestroyed;

            _hud = new HUD();
            _modPanel = new ModSlotPanel();
            _gameOverScreen = new GameOverScreen();
            rlImGui.Setup(true);
            _imguiInitialized = true;

            Console.WriteLine("[GameLoop] All systems initialized.");
        }

        public void RunFrame()
        {
            var prof = Profiler.Instance;
            _frameSw.Restart();

            float frameDt = Raylib.GetFrameTime();
            if (frameDt > MaxAccumulator)
                frameDt = MaxAccumulator;

            _input.Poll();

            HandleGlobalInput();

            prof.Begin("FixedUpdate");
            _accumulator += frameDt;
            while (_accumulator >= FixedDt)
            {
                FixedUpdate(FixedDt);
                _accumulator -= FixedDt;
            }
            prof.End();

            prof.Begin("Update");
            Update(frameDt);
            prof.End();

            _camera.LateUpdate(frameDt);

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= frameDt;
                float t = Math.Clamp(_shakeTimer / _shakeDuration, 0f, 1f);
                float strength = _shakeIntensity * t * t;
                float x = (Random.Shared.NextSingle() * 2f - 1f) * strength;
                float y = (Random.Shared.NextSingle() * 2f - 1f) * strength;
                _camera.SetShakeOffset(new Vector3(x, y, 0f));
                if (_shakeTimer <= 0f)
                    _shakeIntensity = 0f;
            }
            else
            {
                _camera.SetShakeOffset(Vector3.Zero);
            }

            var cam = _camera.Apply();

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            if (_postProcessingAvailable)
            {
                prof.Begin("Render3D");
                _postProcessing.OnResize(screenW, screenH);
                _postProcessing.BeginScene();
                Raylib.ClearBackground(Color.Black);
                Raylib.BeginMode3D(cam);
                Render3D(cam);
                Raylib.EndMode3D();
                Raylib.EndTextureMode();
                prof.End();

                prof.Begin("PostProcess");
                _postProcessing.EndSceneAndComposite();
                prof.End();

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                prof.Begin("FinalBlit");
                _postProcessing.DrawFinalToScreen();
                prof.End();
            }
            else
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                prof.Begin("Render3D");
                Raylib.BeginMode3D(cam);
                Render3D(cam);
                Raylib.EndMode3D();
                prof.End();
            }

            prof.Begin("HUD");
            DrawHUD();
            prof.End();

            if (_pendingScreenshot != null)
            {
                Raylib.TakeScreenshot(_pendingScreenshot);
                _pendingScreenshot = null;
            }

            prof.Begin("SwapBuffers");
            Raylib.EndDrawing();
            prof.End();

            double frameMs = _frameSw.Elapsed.TotalMilliseconds;
            prof.EndFrame(frameMs);
        }

        private void FixedUpdate(float dt)
        {
            var prof = Profiler.Instance;

            prof.Begin("  WarpPhysics");
            _warpManager.Update(dt);
            prof.End();

            prof.Begin("  EnemyUpdate");
            _enemySpawner.Update(dt);
            prof.End();

            prof.Begin("  Projectiles");
            for (int i = 0; i < _projectiles.Count; i++)
                _projectiles[i].Update(dt);
            DrainProjectileSpawnBuffer();
            CleanupDestroyedProjectiles();
            prof.End();
        }

        private void Update(float dt)
        {
            var prof = Profiler.Instance;

            prof.Begin("  GridVisual");
            _gridVisual.Update(dt);
            prof.End();

            _towerPlacement.Update(dt);

            prof.Begin("  Particles");
            _particles.Update(dt);
            prof.End();

            prof.Begin("  Trails");
            _trails.Update(dt);
            prof.End();

            prof.Begin("  Voxels");
            _voxelPool.Update(dt);
            prof.End();

            _impactFlash.Update(dt);
            _pathVisualizer.Update(dt);
            _soundManager.Update();

            UpdateProjectileTrails();

            Vector3 collectTarget = _camera.FocusPoint;
            collectTarget.Y = 0.5f;
            _lootDropper.Update(dt, collectTarget);

            if (_postProcessingAvailable)
            {
                DecayChromaticAberration(dt);
                _postProcessing.ChromaticIntensity = _chromaticDecay;

                DecayBloomPulse(dt);
                _postProcessing.BloomIntensity = _bloomPulse;
            }

            HandlePlacementInput();
        }

        private void AddStarterMods()
        {
            _inventory.AddMod(ModType.Heavy, 3);
            _inventory.AddMod(ModType.Swift, 3);
            _inventory.AddMod(ModType.Homing, 2);
            _inventory.AddMod(ModType.Burn, 2);
            _inventory.AddMod(ModType.Frost, 2);
            _inventory.AddMod(ModType.Split, 1);
            _inventory.AddMod(ModType.OnHit, 2);
            _inventory.AddMod(ModType.OnKill, 1);
        }

        private void TriggerShake(float duration, float intensity)
        {
            if (intensity >= _shakeIntensity || _shakeTimer <= 0f)
            {
                _shakeDuration = duration;
                _shakeIntensity = intensity;
                _shakeTimer = duration;
            }
        }

        private void DecayChromaticAberration(float dt)
        {
            if (_chromaticDecay > 0f)
            {
                _chromaticDecay -= dt * 4f;
                if (_chromaticDecay < 0f) _chromaticDecay = 0f;
            }
        }

        private void DecayBloomPulse(float dt)
        {
            if (_bloomPulse > BaseBloomIntensity)
            {
                _bloomPulse -= dt * 6f;
                if (_bloomPulse < BaseBloomIntensity) _bloomPulse = BaseBloomIntensity;
            }
        }

        private static TowerData DefaultTowerData() => new()
        {
            BaseRange = 10f,
            BaseDamage = 5f,
            FireRate = 2f,
            SlotCount = 5
        };

        private static ModSlotPreset DefaultPreset() => new()
        {
            PresetName = "Default",
            TargetingMode = TargetingMode.First,
            Slots = new List<ModType> { ModType.Heavy, ModType.Swift }
        };

        private static Dictionary<SoundType, SoundConfig> BuildSoundConfigs()
        {
            const string G = "resources/audio/sfx/game/";
            const string U = "resources/audio/sfx/ui/";
            return new Dictionary<SoundType, SoundConfig>
            {
                [SoundType.TowerFire] = new() { FilePaths = new[] { G+"tower_fire_00.wav", G+"tower_fire_01.wav", G+"tower_fire_02.wav", G+"tower_fire_03.wav" }, Volume = 0.5f, PitchVariance = 0.15f, Cooldown = 0.04f, MaxInstances = 6 },
                [SoundType.EnemyHit] = new() { FilePaths = new[] { G+"enemy_hit_00.wav", G+"enemy_hit_01.wav", G+"enemy_hit_02.wav", G+"enemy_hit_03.wav" }, Volume = 0.4f, PitchVariance = 0.2f, Cooldown = 0.03f, MaxInstances = 8 },
                [SoundType.EnemyDeath] = new() { FilePaths = new[] { G+"enemy_death_00.wav", G+"enemy_death_01.wav", G+"enemy_death_02.wav", G+"enemy_death_03.wav" }, Volume = 0.6f, PitchVariance = 0.15f, Cooldown = 0.05f, MaxInstances = 4 },
                [SoundType.ProjectileImpact] = new() { FilePaths = new[] { G+"projectile_impact_00.wav", G+"projectile_impact_01.wav", G+"projectile_impact_02.wav", G+"projectile_impact_03.wav" }, Volume = 0.35f, PitchVariance = 0.2f, Cooldown = 0.03f, MaxInstances = 8 },
                [SoundType.TowerPlace] = new() { FilePaths = new[] { G+"tower_place_01.wav", G+"tower_place_02.wav", G+"tower_place_03.wav" }, Volume = 0.7f, PitchVariance = 0.1f, Cooldown = 0.2f },
                [SoundType.TowerPlaceInvalid] = new() { FilePaths = new[] { G+"tower_place_invalid_00.wav", G+"tower_place_invalid_01.wav" }, Volume = 0.5f, Cooldown = 0.2f },
                [SoundType.TowerHover] = new() { FilePaths = new[] { G+"tower_hover_00.wav" }, Volume = 0.3f, Cooldown = 0.1f },
                [SoundType.ObjectiveHit] = new() { FilePaths = new[] { G+"objective_hit_00.wav", G+"objective_hit_01.wav" }, Volume = 0.7f, Cooldown = 0.1f },
                [SoundType.GameOver] = new() { FilePaths = new[] { G+"game_over_00.wav" }, Volume = 0.8f, Cooldown = 1f },
                [SoundType.LootCollect] = new() { FilePaths = new[] { G+"loot_collect_00.wav", G+"loot_collect_01.wav" }, Volume = 0.5f, PitchVariance = 0.2f, Cooldown = 0.05f, MaxInstances = 4 },
                [SoundType.LootDrop] = new() { FilePaths = new[] { G+"loot_drop_00.wav", G+"loot_drop_01.wav" }, Volume = 0.4f, PitchVariance = 0.15f, Cooldown = 0.05f, MaxInstances = 4 },
                [SoundType.WaveStart] = new() { FilePaths = new[] { U+"wave_start_00.wav", U+"wave_start_01.wav" }, Volume = 0.7f, Cooldown = 1f },
                [SoundType.WaveComplete] = new() { FilePaths = new[] { U+"wave_complete_00.wav", U+"wave_complete_01.wav" }, Volume = 0.7f, Cooldown = 1f },
                [SoundType.UIClick] = new() { FilePaths = new[] { U+"ui_click_01.wav", U+"ui_click_02.wav", U+"ui_click_03.wav" }, Volume = 0.5f, PitchVariance = 0.1f, Cooldown = 0.05f },
                [SoundType.UIHover] = new() { FilePaths = new[] { U+"ui_hover_00.wav", U+"ui_hover_01.wav" }, Volume = 0.25f, Cooldown = 0.08f },
                [SoundType.UIAnnounce] = new() { FilePaths = new[] { U+"ui_announce_00.wav" }, Volume = 0.6f, Cooldown = 0.5f },
                [SoundType.EditorOpen] = new() { FilePaths = new[] { U+"editor_open_00.wav", U+"editor_open_01.wav" }, Volume = 0.5f, Cooldown = 0.2f },
                [SoundType.EditorClose] = new() { FilePaths = new[] { U+"editor_close_00.wav", U+"editor_close_01.wav" }, Volume = 0.5f, Cooldown = 0.2f },
                [SoundType.NodeGrab] = new() { FilePaths = new[] { U+"node_grab_00.wav", U+"node_grab_01.wav" }, Volume = 0.4f, PitchVariance = 0.1f, Cooldown = 0.05f },
                [SoundType.NodeDrop] = new() { FilePaths = new[] { U+"node_drop_00.wav", U+"node_drop_01.wav" }, Volume = 0.4f, PitchVariance = 0.1f, Cooldown = 0.05f },
                [SoundType.NodeRemove] = new() { FilePaths = new[] { U+"node_remove_00.wav", U+"node_remove_01.wav" }, Volume = 0.4f, Cooldown = 0.1f },
                [SoundType.PortConnect] = new() { FilePaths = new[] { U+"port_connect_00.wav", U+"port_connect_01.wav" }, Volume = 0.45f, Cooldown = 0.1f },
                [SoundType.PortDisconnect] = new() { FilePaths = new[] { U+"port_disconnect_00.wav", U+"port_disconnect_01.wav" }, Volume = 0.4f, Cooldown = 0.1f },
                [SoundType.UIDragStart] = new() { FilePaths = new[] { U+"ui_drag_start_00.wav", U+"ui_drag_start_01.wav" }, Volume = 0.35f, Cooldown = 0.05f },
                [SoundType.UIDragHover] = new() { FilePaths = new[] { U+"ui_drag_hover_00.wav" }, Volume = 0.25f, Cooldown = 0.08f },
                [SoundType.UIDropFail] = new() { FilePaths = new[] { U+"ui_drop_fail_00.wav", U+"ui_drop_fail_01.wav" }, Volume = 0.4f, Cooldown = 0.1f },
                [SoundType.UIDropdownOpen] = new() { FilePaths = new[] { U+"ui_dropdown_open_00.wav" }, Volume = 0.4f, Cooldown = 0.1f },
                [SoundType.UIDropdownSelect] = new() { FilePaths = new[] { U+"ui_dropdown_select_00.wav" }, Volume = 0.4f, Cooldown = 0.05f },
            };
        }

        private static List<WaveDefinition> CreateTestWaves()
        {
            var scout = new EnemyData
            {
                Name = "Scout",
                MaxHP = 20f,
                MoveSpeed = 3f,
                ObjectiveDamage = 1f,
                Scale = new Vector3(0.6f, 0.6f, 0.6f),
                Color = 0xFF4444FF
            };

            var runner = new EnemyData
            {
                Name = "Runner",
                MaxHP = 15f,
                MoveSpeed = 5f,
                ObjectiveDamage = 1f,
                Scale = new Vector3(0.5f, 0.5f, 0.5f),
                Color = 0x44FF44FF
            };

            var tank = new EnemyData
            {
                Name = "Tank",
                MaxHP = 60f,
                MoveSpeed = 1.5f,
                ObjectiveDamage = 3f,
                Scale = new Vector3(0.9f, 0.9f, 0.9f),
                Color = 0xFF8800FF
            };

            var elite = new EnemyData
            {
                Name = "Elite",
                MaxHP = 100f,
                MoveSpeed = 2f,
                ObjectiveDamage = 5f,
                Scale = new Vector3(1.0f, 1.0f, 1.0f),
                Color = 0xAA00FFFF
            };

            var swarm = new EnemyData
            {
                Name = "Swarm",
                MaxHP = 8f,
                MoveSpeed = 4f,
                ObjectiveDamage = 0.5f,
                Scale = new Vector3(0.35f, 0.35f, 0.35f),
                Color = 0xFFFF00FF
            };

            return new List<WaveDefinition>
            {
                new()
                {
                    Entries = new List<SpawnEntry>
                    {
                        new() { Enemy = scout, Count = 8, SpawnInterval = 0.35f, DelayBeforeGroup = 0f }
                    }
                },
                new()
                {
                    Entries = new List<SpawnEntry>
                    {
                        new() { Enemy = scout, Count = 10, SpawnInterval = 0.3f, DelayBeforeGroup = 0f },
                        new() { Enemy = runner, Count = 6, SpawnInterval = 0.25f, DelayBeforeGroup = 0.5f }
                    }
                },
                new()
                {
                    Entries = new List<SpawnEntry>
                    {
                        new() { Enemy = scout, Count = 8, SpawnInterval = 0.25f, DelayBeforeGroup = 0f },
                        new() { Enemy = tank, Count = 4, SpawnInterval = 0.5f, DelayBeforeGroup = 0.5f },
                        new() { Enemy = runner, Count = 8, SpawnInterval = 0.2f, DelayBeforeGroup = 0.5f }
                    }
                },
                new()
                {
                    Entries = new List<SpawnEntry>
                    {
                        new() { Enemy = swarm, Count = 20, SpawnInterval = 0.1f, DelayBeforeGroup = 0f },
                        new() { Enemy = tank, Count = 5, SpawnInterval = 0.5f, DelayBeforeGroup = 0.5f },
                        new() { Enemy = elite, Count = 2, SpawnInterval = 0.5f, DelayBeforeGroup = 0.5f }
                    }
                },
                new()
                {
                    Entries = new List<SpawnEntry>
                    {
                        new() { Enemy = runner, Count = 15, SpawnInterval = 0.15f, DelayBeforeGroup = 0f },
                        new() { Enemy = tank, Count = 6, SpawnInterval = 0.4f, DelayBeforeGroup = 0.5f },
                        new() { Enemy = swarm, Count = 25, SpawnInterval = 0.08f, DelayBeforeGroup = 0.5f },
                        new() { Enemy = elite, Count = 4, SpawnInterval = 0.5f, DelayBeforeGroup = 0.5f }
                    }
                }
            };
        }

        public void Shutdown()
        {
            _projectiles.Clear();
            _projectileSpawnBuffer.Clear();
            _projectileTrails.Clear();

            _waveManager.Shutdown();
            _towerPlacement.Shutdown();
            _lootDropper.Shutdown();
            _objective.Shutdown();

            if (_imguiInitialized) rlImGui.Shutdown();
            _gridVisual.Shutdown();
            _warpManager.Shutdown();
            _gridManager.Shutdown();

            _inventory.Shutdown();
            _soundManager.Shutdown();
            _voxelPool.Shutdown();
            _impactFlash.Shutdown();

            if (_postProcessingAvailable)
                _postProcessing.Shutdown();

            if (_outlineShaderLoaded)
                Raylib.UnloadShader(_outlineShader);
            WireframeMeshes.Shutdown();

            _gameManager.Shutdown();
            _gameStats.Shutdown();

            EnemyRegistry.Clear();
            ServiceLocator.Clear();
        }
    }
}
