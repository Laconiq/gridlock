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

        private readonly Dictionary<int, int> _projectileTrails = new();

        private VoxelPool _voxelPool = null!;
        private readonly VoxelDeathEffect _voxelDeath = new();
        private ImpactFlash _impactFlash = null!;
        private DamageTextSystem _damageText = null!;
        private readonly LineBatch _lineBatch = new();

        private readonly Dictionary<int, float> _towerSpinAngles = new();
        private readonly Dictionary<int, float> _towerBobPhases = new();

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
        private Camera3D _lastCamera;

        private readonly Stopwatch _frameSw = Stopwatch.StartNew();
        private string? _pendingScreenshot;

        public void RequestScreenshot(string path) => _pendingScreenshot = path;

        public void StartBenchmark()
        {
            BenchmarkRunner.Setup(_gridManager, _towerPlacement, _gameManager, _gameStats);
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

            _inventory = new PlayerInventory();
            _inventory.Init();
            AddStarterMods();

            var defaultTowerData = DefaultTowerData();
            var defaultPreset = DefaultPreset();
            _towerPlacement = new TowerPlacement(_gridManager, defaultTowerData, defaultPreset);
            _towerPlacement.OnTowerPlaced += OnTowerPlaced;
            ModProjectile.OnProjectileCreated = RegisterProjectile;
            _inventory.SetTowerSource(_towerPlacement.PlacedTowers);

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

            _damageText = new DamageTextSystem();
            _damageText.Init();

            _soundManager = new SoundManager();
            _soundManager.Init(new Dictionary<SoundType, SoundConfig>());
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

            _camera.ZoomEnabled = !_modPanel.IsOpen;
            _camera.InputEnabled = !ImGuiNET.ImGui.GetIO().WantCaptureMouse;
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
            _lastCamera = cam;

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

            // Freeze gameplay simulation on game over; visual systems (warp above,
            // particles/trails/voxels in Update) keep animating the death VFX.
            if (_gameManager.CurrentState == GameState.GameOver)
                return;

            prof.Begin("  EnemyUpdate");
            _enemySpawner.Update(dt);
            prof.End();

            prof.Begin("  SpatialHash");
            EnemyRegistry.RebuildSpatial();
            prof.End();

            // Towers tick at the fixed rate so fire rate doesn't depend on the render framerate,
            // and after the spatial rebuild so target selection sees this step's positions.
            prof.Begin("  Towers");
            _towerPlacement.Update(dt);
            prof.End();

            prof.Begin("  Projectiles");
            for (int i = 0; i < _projectiles.Count; i++)
                _projectiles[i].Update(dt);
            CleanupDestroyedProjectiles();
            prof.End();
        }

        private void Update(float dt)
        {
            var prof = Profiler.Instance;

            prof.Begin("  GridVisual");
            _gridVisual.Update(dt);
            prof.End();

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
            _damageText.Update(dt);
            _pathVisualizer.Update(dt);
            _soundManager.Update();
            _soundManager.SetCameraInfo(_camera.FocusPoint.X, _camera.OrthoSize * 3.4f);

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

            if (_selectedTower != null && !_modPanel.IsOpen)
                _selectedTower = null;

            HandlePlacementInput();
        }

        private void AddStarterMods()
        {
            foreach (var type in Enum.GetValues<ModType>())
                _inventory.AddMod(type, 5);
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
            TargetingMode = TargetingMode.First,
            Slots = new List<ModType> { ModType.Heavy, ModType.Swift }
        };

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
            _damageText.Shutdown();

            if (_postProcessingAvailable)
                _postProcessing.Shutdown();

            _gameManager.Shutdown();
            _gameStats.Shutdown();

            ModProjectile.OnProjectileCreated = null;
            EnemyRegistry.Clear();
            ServiceLocator.Clear();
        }
    }
}
