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
    public sealed class GameLoop
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

            // Find cells adjacent to the path
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

        private void HandleGlobalInput()
        {
            if (_input.SpacePressed)
            {
                if (_gameManager.CurrentState == GameState.Preparing)
                {
                    _gameManager.SetState(GameState.Wave);
                    _gameStats.SetWave(_waveManager.CurrentWave + 1);
                }
                else if (_gameManager.CurrentState == GameState.GameOver)
                {
                    ResetGame();
                }
            }

            if (_input.EscapePressed)
            {
                if (_modPanel.IsOpen)
                    _modPanel.Close();
                else if (_gameManager.CurrentState == GameState.GameOver)
                    ResetGame();
            }

            if (_input.RightClicked && _modPanel.IsOpen)
                _modPanel.Close();
        }

        private void HandlePlacementInput()
        {
            bool panelOpen = _modPanel != null && _modPanel.IsOpen;
            bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

            if (_camera.ScreenToGroundPoint(_input.MouseScreenPos, out var groundPoint))
            {
                if (!panelOpen)
                    _towerPlacement.UpdatePreview(groundPoint);

                if (_input.LeftClicked && _gameManager.CurrentState == GameState.Preparing && !imguiWantsMouse)
                {
                    if (panelOpen)
                    {
                        _modPanel.Close();
                        _selectedTower = null;
                    }
                    else
                    {
                        var clickedTower = _towerPlacement.TryClickTower(groundPoint);
                        if (clickedTower != null)
                        {
                            _selectedTower = clickedTower;
                            _modPanel.Open(clickedTower, _inventory);
                        }
                        else
                        {
                            var placed = _towerPlacement.TryPlace(groundPoint, isOverUI: false);
                            if (placed != null)
                                _selectedTower = placed;
                        }
                    }
                }
            }
        }

        private void OnTowerPlaced(Tower tower)
        {
            tower.Executor.OnProjectileSpawned += OnProjectileSpawned;

            if (_warpManager.Initialized)
            {
                _warpManager.Shockwave(tower.Position, 5f, 3f, new Color(0, 255, 179, 255));
            }

            _particles.Burst(tower.Position + new Vector3(0, 0.5f, 0), 20, 3f, 5f, 0.8f,
                new Color(0, 255, 179, 200));

            _soundManager.Play(SoundType.TowerPlace, worldPos: tower.Position);
        }

        private void OnEnemyKilled(Vector3 position)
        {
            if (_warpManager.Initialized)
                _warpManager.Shockwave(position, 5.5f, 6f, new Color(255, 38, 38, 255));

            _particles.BurstSphere(position, 20, 5f, 5f, 0.6f, new Color(255, 38, 38, 255));

            var deathColor = new Color((byte)255, (byte)80, (byte)80, (byte)255);
            var voxelDeath = new VoxelDeathEffect();
            voxelDeath.Precompute(0.8f);
            voxelDeath.OnDeath(position, deathColor);

            _impactFlash.Spawn(position, new Color((byte)255, (byte)100, (byte)50, (byte)255), 0.6f, 0.2f);

            TriggerShake(0.12f, 0.25f);

            if (_postProcessingAvailable)
            {
                _chromaticDecay = MathF.Max(_chromaticDecay, 0.2f);
                _bloomPulse = MathF.Max(_bloomPulse, BaseBloomIntensity + 2f);
            }

            _gameStats.AddKill();
        }

        private void OnProjectileSpawned(ModProjectile projectile)
        {
            _projectiles.Add(projectile);
            projectile.OnDestroyed += OnProjectileDestroyed;
            projectile.OnSpawnRequested += OnSubProjectileRequested;

            var tags = projectile.Context.Tags;
            var projectileColor = GetProjectileColor(tags);
            float trailWidth = 0.1f + projectile.Context.Damage / 40f * 0.15f;
            var startColor = new Color((byte)255, (byte)255, (byte)255, (byte)230);
            var endColor = new Color((byte)(projectileColor.R / 3), (byte)(projectileColor.G / 3), (byte)(projectileColor.B / 3), (byte)0);
            int trailId = _trails.CreateTrail(0.2f, trailWidth, startColor, endColor, projectileColor);
            _projectileTrails[projectile.GetHashCode()] = trailId;

            _particles.Burst(projectile.Position, 8, 7.5f, 0f, 0.09f, projectileColor, 18f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 1.5f, 2f, new Color(0, 255, 255, 255));

            _soundManager.Play(SoundType.TowerFire, worldPos: projectile.Position);
        }

        private void OnProjectileDestroyed(ModProjectile projectile)
        {
            var projColor = GetProjectileColor(projectile.Context.Tags);
            _particles.BurstSphere(projectile.Position, 12, 4f, 5f, 0.15f, projColor);

            _impactFlash.Spawn(projectile.Position, projColor, 0.5f, 0.15f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 3f, 3f, new Color(255, 102, 26, 255));

            if (_projectileTrails.TryGetValue(projectile.GetHashCode(), out int trailId))
            {
                _trails.DestroyTrail(trailId);
                _projectileTrails.Remove(projectile.GetHashCode());
            }

            _soundManager.Play(SoundType.ProjectileImpact, worldPos: projectile.Position);

            if (_postProcessingAvailable)
                _bloomPulse = MathF.Max(_bloomPulse, BaseBloomIntensity + 1f);

            TriggerShake(0.05f, 0.08f);
        }

        private void OnSubProjectileRequested(ModProjectile parent, SpawnRequest request)
        {
            var sub = new ModProjectile();
            var pipeline = request.Pipeline?.Clone() ?? parent.Context.OwnerPipeline?.Clone();
            if (pipeline == null) return;

            var ctx = parent.Context.CloneForSub(request.DamageScale);
            sub.Initialize(pipeline, ctx, request.Target!, request.Origin);

            if (!sub.IsDestroyed)
            {
                if (request.Direction != Vector3.Zero)
                    sub.OverrideDirection(request.Direction);

                _projectileSpawnBuffer.Add(sub);
            }
        }

        private void DrainProjectileSpawnBuffer()
        {
            foreach (var proj in _projectileSpawnBuffer)
            {
                _projectiles.Add(proj);
                proj.OnDestroyed += OnProjectileDestroyed;
                proj.OnSpawnRequested += OnSubProjectileRequested;

                var tags = proj.Context.Tags;
                var trailColor = GetProjectileColor(tags);
                float subTrailWidth = 0.08f + proj.Context.Damage / 40f * 0.1f;
                var subStartColor = new Color((byte)255, (byte)255, (byte)255, (byte)200);
                var subEndColor = new Color((byte)(trailColor.R / 3), (byte)(trailColor.G / 3), (byte)(trailColor.B / 3), (byte)0);
                int trailId = _trails.CreateTrail(0.25f, subTrailWidth, subStartColor, subEndColor, trailColor);
                _projectileTrails[proj.GetHashCode()] = trailId;
            }
            _projectileSpawnBuffer.Clear();
        }

        private void CleanupDestroyedProjectiles()
        {
            _projectileRemovalBuffer.Clear();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (_projectiles[i].IsDestroyed)
                    _projectileRemovalBuffer.Add(_projectiles[i]);
            }
            foreach (var proj in _projectileRemovalBuffer)
            {
                _projectiles.Remove(proj);
                proj.OnDestroyed -= OnProjectileDestroyed;
                proj.OnSpawnRequested -= OnSubProjectileRequested;
            }
        }

        private void UpdateProjectileTrails()
        {
            foreach (var proj in _projectiles)
            {
                if (proj.IsDestroyed) continue;
                if (_projectileTrails.TryGetValue(proj.GetHashCode(), out int trailId))
                {
                    float warpY = _warpManager.Initialized
                        ? _warpManager.GetWarpOffset(proj.Position.X, proj.Position.Z) : 0f;
                    var warpedPos = new Vector3(proj.Position.X, proj.Position.Y + warpY, proj.Position.Z);
                    _trails.AddPoint(trailId, warpedPos);
                }
            }
        }

        private void OnWaveCleared(int waveNumber)
        {
            _gameStats.SetWave(waveNumber);

            _particles.BurstSphere(_gridManager.ObjectivePosition + new Vector3(0, 2, 0),
                40, 6f, 3f, 1.5f, new Color(0, 255, 200, 255));

            if (_warpManager.Initialized)
                _warpManager.Shockwave(_gridManager.ObjectivePosition, 5f, 10f, new Color(0, 255, 200, 255));

            _soundManager.Play(SoundType.WaveComplete);

            Console.WriteLine($"[GameLoop] Wave {waveNumber} cleared!");
        }

        private void OnObjectiveDestroyed()
        {
            _gameManager.SetState(GameState.GameOver);
            _soundManager.Play(SoundType.GameOver);

            if (_postProcessingAvailable)
            {
                _chromaticDecay = 0.5f;
                _bloomPulse = BaseBloomIntensity + 3f;
            }

            TriggerShake(0.5f, 0.6f);
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (current == GameState.Wave)
            {
                _soundManager.Play(SoundType.WaveStart);
            }

            if (current == GameState.GameOver)
            {
                _gameOverScreen.Reset();

                if (_warpManager.Initialized)
                    _warpManager.Shockwave(_gridManager.ObjectivePosition, 8f, 15f, new Color(255, 0, 0, 255));

                _particles.BurstSphere(_gridManager.ObjectivePosition, 60, 8f, 5f, 2f,
                    new Color(255, 50, 50, 255));
            }
        }

        private void ResetGame()
        {
            _projectiles.Clear();
            _projectileSpawnBuffer.Clear();
            _projectileTrails.Clear();
            _trails.Clear();
            _particles.Clear();
            _voxelPool.Clear();
            _impactFlash.Clear();
            _towerSpinAngles.Clear();
            _towerBobPhases.Clear();
            _bloomPulse = BaseBloomIntensity;
            _chromaticDecay = 0f;
            _enemySpawner.Clear();
            EnemyRegistry.Clear();
            _lootDropper.Clear();
            _waveManager.ResetWaves();
            _selectedTower = null;
            if (_modPanel != null && _modPanel.IsOpen) _modPanel.Close();
            _objective.ResetHP();
            _gridManager.ResetCells();
            _gameManager.ResetGame();
        }

        private void Render3D(Camera3D cam)
        {
            var prof = Profiler.Instance;

            prof.Begin("  R.Grid");
            _gridVisual.Render();
            if (!_gridVisual.HasShader)
                DrawFallbackGrid();
            prof.End();

            prof.Begin("  R.Path");
            _pathVisualizer.Render(cam);
            prof.End();

            prof.Begin("  R.Towers");
            DrawTowers();
            prof.End();

            prof.Begin("  R.Enemies");
            DrawEnemies();
            prof.End();

            prof.Begin("  R.Projectiles");
            DrawProjectiles();
            prof.End();

            DrawPickups();
            DrawPlacementPreview();

            prof.Begin("  R.Voxels");
            _voxelPool.Render();
            prof.End();

            prof.Begin("  R.ImpactFlash");
            _impactFlash.Render();
            prof.End();

            prof.Begin("  R.Particles");
            _particles.Render();
            prof.End();

            prof.Begin("  R.Trails");
            _trails.Render(cam);
            prof.End();
        }

        private void DrawFallbackGrid()
        {
            var def = _gridManager.Definition;
            var origin = _gridManager.GridOrigin;
            float totalW = def.Width * def.CellSize;
            float totalH = def.Height * def.CellSize;

            for (int y = 0; y < def.Height; y++)
            {
                for (int x = 0; x < def.Width; x++)
                {
                    var cell = _gridManager.GetRuntimeCell(x, y);
                    if (cell == CellType.Blocked && _gridManager.Definition.GetCell(x, y) == CellType.Blocked) continue;

                    var worldPos = _gridManager.GridToWorld(new Vector2Int(x, y));
                    float warpY = _warpManager.Initialized
                        ? _warpManager.GetWarpOffset(worldPos.X, worldPos.Z)
                        : 0f;

                    var color = cell switch
                    {
                        CellType.Path => new Color(255, 0, 180, 40),
                        CellType.TowerSlot => new Color(0, 255, 100, 25),
                        CellType.Spawn => new Color(0, 255, 255, 60),
                        CellType.Objective => new Color(255, 255, 0, 60),
                        CellType.Blocked => new Color(100, 100, 100, 20),
                        _ => new Color(40, 40, 40, 15),
                    };

                    var rlPos = new Vector3(worldPos.X, 0.01f + warpY, worldPos.Z);
                    var rlSize = new Vector3(def.CellSize * 0.95f, 0.02f, def.CellSize * 0.95f);
                    Raylib.DrawCubeV(rlPos, rlSize, color);
                }
            }

            for (int x = 0; x <= def.Width; x++)
            {
                float wx = origin.X + x * def.CellSize;
                var a = new Vector3(wx, 0f, origin.Z);
                var b = new Vector3(wx, 0f, origin.Z + totalH);
                Raylib.DrawLine3D(a, b, new Color(60, 60, 60, 80));
            }
            for (int y = 0; y <= def.Height; y++)
            {
                float wz = origin.Z + y * def.CellSize;
                var a = new Vector3(origin.X, 0f, wz);
                var b = new Vector3(origin.X + totalW, 0f, wz);
                Raylib.DrawLine3D(a, b, new Color(60, 60, 60, 80));
            }
        }

        private void DrawTowers()
        {
            float time = (float)Raylib.GetTime();
            float dt = Raylib.GetFrameTime();

            foreach (var tower in _towerPlacement.PlacedTowers)
            {
                int id = tower.EntityId;

                if (!_towerBobPhases.ContainsKey(id))
                    _towerBobPhases[id] = Random.Shared.NextSingle() * MathF.Tau;
                if (!_towerSpinAngles.ContainsKey(id))
                    _towerSpinAngles[id] = 0f;

                _towerSpinAngles[id] += 30f * dt;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(tower.Position.X, tower.Position.Z)
                    : 0f;

                var pos = new Vector3(tower.Position.X, 0.5f + warpY, tower.Position.Z);
                bool selected = _selectedTower == tower;

                var baseWireColor = selected
                    ? new Color((byte)0, (byte)255, (byte)200, (byte)255)
                    : new Color((byte)0, (byte)180, (byte)255, (byte)200);

                float bobPhase = _towerBobPhases[id];
                float bob = MathF.Sin(time * 2f + bobPhase) * 0.06f;
                var turretPos = new Vector3(tower.Position.X, 1.2f + warpY + bob, tower.Position.Z);

                var turretColor = selected
                    ? new Color((byte)0, (byte)255, (byte)200, (byte)255)
                    : new Color((byte)0, (byte)220, (byte)255, (byte)220);

                float spin = _towerSpinAngles[id] * MathF.PI / 180f;

                if (_outlineShaderLoaded)
                {
                    DrawWireframeCube(pos, 1.6f, 1.0f, 1.6f, baseWireColor, 3.0f, 1.5f);

                    Raylib.BeginBlendMode(BlendMode.Additive);
                    DrawWireframeCube(pos, 1.75f, 1.15f, 1.75f,
                        new Color(baseWireColor.R, baseWireColor.G, baseWireColor.B, (byte)50), 2.0f, 2.5f);
                    Raylib.EndBlendMode();

                    DrawWireframeOctahedron(turretPos, 0.35f, 0.63f, spin, turretColor, 3.0f, 1.5f);

                    Raylib.BeginBlendMode(BlendMode.Additive);
                    DrawWireframeOctahedron(turretPos, 0.45f, 0.78f, spin,
                        new Color(turretColor.R, turretColor.G, turretColor.B, (byte)45), 2.0f, 2.5f);
                    Raylib.DrawSphere(turretPos, 0.12f,
                        new Color(turretColor.R, turretColor.G, turretColor.B, (byte)30));
                    Raylib.EndBlendMode();
                }
                else
                {
                    Raylib.DrawCubeWires(pos, 1.6f, 1.0f, 1.6f, baseWireColor);
                    Raylib.DrawCubeWires(pos, 1.58f, 0.98f, 1.58f, baseWireColor);
                    Raylib.DrawCubeWires(pos, 1.62f, 1.02f, 1.62f, baseWireColor);

                    Raylib.BeginBlendMode(BlendMode.Additive);
                    Raylib.DrawCubeWires(pos, 1.75f, 1.15f, 1.75f,
                        new Color(baseWireColor.R, baseWireColor.G, baseWireColor.B, (byte)50));
                    Raylib.EndBlendMode();

                    DrawOctahedronWiresRotated(turretPos, 0.35f, 0.63f, spin, turretColor);
                    DrawOctahedronWiresRotated(turretPos, 0.34f, 0.62f, spin, turretColor);
                    DrawOctahedronWiresRotated(turretPos, 0.36f, 0.64f, spin, turretColor);

                    Raylib.BeginBlendMode(BlendMode.Additive);
                    DrawOctahedronWiresRotated(turretPos, 0.45f, 0.78f, spin,
                        new Color(turretColor.R, turretColor.G, turretColor.B, (byte)45));
                    Raylib.DrawSphere(turretPos, 0.12f,
                        new Color(turretColor.R, turretColor.G, turretColor.B, (byte)30));
                    Raylib.EndBlendMode();
                }

                if (selected && _gameManager.CurrentState == GameState.Preparing)
                {
                    int segments = 64;
                    float range = tower.Data.BaseRange;
                    Raylib.BeginBlendMode(BlendMode.Additive);
                    for (int i = 0; i < segments; i++)
                    {
                        float a1 = (float)i / segments * MathF.Tau;
                        float a2 = (float)(i + 1) / segments * MathF.Tau;
                        var p1 = new Vector3(pos.X + MathF.Cos(a1) * range, 0.02f, pos.Z + MathF.Sin(a1) * range);
                        var p2 = new Vector3(pos.X + MathF.Cos(a2) * range, 0.02f, pos.Z + MathF.Sin(a2) * range);
                        Raylib.DrawLine3D(p1, p2, new Color(0, 255, 200, 50));
                    }
                    Raylib.EndBlendMode();
                }
            }
        }

        private void DrawWireframeCube(Vector3 position, float sx, float sy, float sz,
            Color color, float emission, float edgeWidth)
        {
            float[] colorVec = { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };
            Raylib.SetShaderValue(_outlineShader, _locLineColor, colorVec, ShaderUniformDataType.Vec4);
            Raylib.SetShaderValue(_outlineShader, _locEmissionIntensity, emission, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(_outlineShader, _locEdgeWidth, edgeWidth, ShaderUniformDataType.Float);

            var transform = Matrix4x4.CreateScale(sx, sy, sz)
                * Matrix4x4.CreateTranslation(position);
            Raylib.DrawMesh(WireframeMeshes.Cube, _outlineMaterial, transform);
        }

        private void DrawWireframeOctahedron(Vector3 position, float radiusH, float radiusV,
            float angleY, Color color, float emission, float edgeWidth)
        {
            float[] colorVec = { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };
            Raylib.SetShaderValue(_outlineShader, _locLineColor, colorVec, ShaderUniformDataType.Vec4);
            Raylib.SetShaderValue(_outlineShader, _locEmissionIntensity, emission, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(_outlineShader, _locEdgeWidth, edgeWidth, ShaderUniformDataType.Float);

            float scaleH = radiusH / 0.5f;
            float scaleV = radiusV / 1.0f;
            var transform = Matrix4x4.CreateScale(scaleH, scaleV, scaleH)
                * Matrix4x4.CreateRotationY(angleY)
                * Matrix4x4.CreateTranslation(position);
            Raylib.DrawMesh(WireframeMeshes.Octahedron, _outlineMaterial, transform);
        }

        private void DrawWireframePyramid(Vector3 position, float scale,
            Color color, float emission, float edgeWidth)
        {
            float[] colorVec = { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };
            Raylib.SetShaderValue(_outlineShader, _locLineColor, colorVec, ShaderUniformDataType.Vec4);
            Raylib.SetShaderValue(_outlineShader, _locEmissionIntensity, emission, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(_outlineShader, _locEdgeWidth, edgeWidth, ShaderUniformDataType.Float);

            var transform = Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateTranslation(position);
            Raylib.DrawMesh(WireframeMeshes.Cone, _outlineMaterial, transform);
        }

        private void DrawEnemies()
        {
            float time = (float)Raylib.GetTime();

            foreach (var enemy in _enemySpawner.ActiveEnemies)
            {
                if (!enemy.IsAlive) continue;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(enemy.Position.X, enemy.Position.Z)
                    : 0f;

                var pos = new Vector3(enemy.Position.X, enemy.Position.Y + warpY, enemy.Position.Z);
                float scale = enemy.Data.Scale.X;

                var baseColor = UintToColor(enemy.Data.Color);
                Color color;
                float hitElapsed = time - enemy.Health.LastHitTime;
                if (enemy.Health.LastHitTime >= 0f && hitElapsed < 0.1f)
                {
                    float t = hitElapsed / 0.1f;
                    byte lr = (byte)(255 + (baseColor.R - 255) * t);
                    byte lg = (byte)(255 + (baseColor.G - 255) * t);
                    byte lb = (byte)(255 + (baseColor.B - 255) * t);
                    color = new Color(lr, lg, lb, (byte)255);
                }
                else
                {
                    color = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)255);
                }

                NeonWireframe.PyramidThick(pos, scale, color);

                float glowPulse = 0.85f + 0.15f * MathF.Sin(time * 4f + enemy.EntityId * 1.7f);
                NeonWireframe.PyramidGlow(pos, scale * 1.15f * glowPulse, baseColor, 40);

                float hpPct = enemy.Health.CurrentHP / enemy.Health.MaxHP;
                float barWidth = scale * 1.2f;
                var barPos = new Vector3(pos.X - barWidth * 0.5f, pos.Y + scale * 0.8f, pos.Z);
                var barEnd = new Vector3(barPos.X + barWidth, barPos.Y, barPos.Z);
                var barFilled = new Vector3(barPos.X + barWidth * hpPct, barPos.Y, barPos.Z);

                Raylib.DrawLine3D(barPos, barEnd, new Color(60, 0, 0, 200));
                Raylib.DrawLine3D(barPos, barFilled, new Color(255, 50, 50, 255));
            }

        }

        private void DrawProjectiles()
        {
            float time = (float)Raylib.GetTime();

            foreach (var proj in _projectiles)
            {
                if (proj.IsDestroyed) continue;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(proj.Position.X, proj.Position.Z)
                    : 0f;

                var pos = new Vector3(proj.Position.X, proj.Position.Y + warpY, proj.Position.Z);
                var color = GetProjectileColor(proj.Context.Tags);

                float baseDamage = proj.Context.Damage;
                float radius = MathF.Max(0.08f, MathF.Min(0.25f, 0.1f + baseDamage / 40f * 0.15f));

                bool hasElement = proj.Context.Tags != ModTags.None;
                float pulse = hasElement
                    ? 0.9f + 0.1f * MathF.Sin(time * 6f + proj.GetHashCode() * 0.3f)
                    : 1f;
                float r = radius * pulse;

                WireframeMeshes.DrawSphere(pos, r, color);
            }

            Raylib.BeginBlendMode(BlendMode.Additive);
            foreach (var proj in _projectiles)
            {
                if (proj.IsDestroyed) continue;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(proj.Position.X, proj.Position.Z)
                    : 0f;

                var pos = new Vector3(proj.Position.X, proj.Position.Y + warpY, proj.Position.Z);
                var color = GetProjectileColor(proj.Context.Tags);

                float baseDamage = proj.Context.Damage;
                float radius = MathF.Max(0.08f, MathF.Min(0.25f, 0.1f + baseDamage / 40f * 0.15f));

                bool hasElement = proj.Context.Tags != ModTags.None;
                float pulse = hasElement
                    ? 0.9f + 0.1f * MathF.Sin(time * 6f + proj.GetHashCode() * 0.3f)
                    : 1f;
                float r = radius * pulse;

                WireframeMeshes.DrawSphere(pos, r * 1.4f,
                    new Color(color.R, color.G, color.B, (byte)25));
                WireframeMeshes.DrawSphere(pos, r * 2.0f,
                    new Color(color.R, color.G, color.B, (byte)10));
            }
            Raylib.EndBlendMode();
        }

        private void DrawPickups()
        {
            float time = (float)Raylib.GetTime();

            foreach (var pickup in _lootDropper.ActivePickups)
            {
                if (pickup.Collected || pickup.Expired) continue;

                var pos = new Vector3(pickup.Position.X, pickup.Position.Y, pickup.Position.Z);
                float s = pickup.Scale;
                var color = GetRarityColor(pickup.Rarity);

                float bob = MathF.Sin(time * 3f + pos.X * 2f + pos.Z * 3f) * 0.05f;
                var drawPos = new Vector3(pos.X, pos.Y + bob, pos.Z);

                Raylib.DrawCube(drawPos, s, s, s, color);
                Raylib.DrawCubeWires(drawPos, s * 1.1f, s * 1.1f, s * 1.1f, new Color(255, 255, 255, 120));

                Raylib.BeginBlendMode(BlendMode.Additive);
                Raylib.DrawCube(drawPos, s * 1.4f, s * 1.4f, s * 1.4f,
                    new Color(color.R, color.G, color.B, (byte)30));
                Raylib.EndBlendMode();
            }
        }

        private void DrawPlacementPreview()
        {
            if (!_towerPlacement.IsPreviewVisible) return;
            if (_modPanel != null && _modPanel.IsOpen) return;

            var pos = _towerPlacement.PreviewPosition;
            float warpY = _warpManager.Initialized
                ? _warpManager.GetWarpOffset(pos.X, pos.Z)
                : 0f;

            var drawPos = new Vector3(pos.X, 0.5f + warpY, pos.Z);

            var color = _towerPlacement.IsPreviewValid
                ? new Color((byte)0, (byte)255, (byte)100, (byte)80)
                : new Color((byte)255, (byte)50, (byte)50, (byte)80);

            var wireColor = _towerPlacement.IsPreviewValid
                ? new Color((byte)0, (byte)255, (byte)100, (byte)180)
                : new Color((byte)255, (byte)50, (byte)50, (byte)180);

            Raylib.DrawCube(drawPos, 1.6f, 1.0f, 1.6f, color);
            Raylib.DrawCubeWires(drawPos, 1.7f, 1.1f, 1.7f, wireColor);

            float pulse = 0.8f + 0.2f * MathF.Sin((float)Raylib.GetTime() * 4f);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawCubeWires(drawPos, 1.8f * pulse, 1.2f * pulse, 1.8f * pulse,
                new Color(wireColor.R, wireColor.G, wireColor.B, (byte)35));
            Raylib.EndBlendMode();
        }

        private void DrawHUD()
        {
            var state = _gameManager.CurrentState;

            _hud.Render(state,
                _waveManager.CurrentWave, _waveManager.TotalWaves,
                _gameStats.TotalKills,
                _objective.CurrentHP, _objective.MaxHP,
                _waveManager.EnemiesRemaining,
                _towerPlacement.PlacedTowers.Count, 5);

            if (_hud.WaveStartRequested && state == GameState.Preparing)
            {
                _gameManager.SetState(GameState.Wave);
                _gameStats.SetWave(_waveManager.CurrentWave + 1);
            }

            bool needImGui = _modPanel.IsOpen || state == GameState.GameOver;

            if (needImGui && _imguiInitialized)
            {
                float dt = Raylib.GetFrameTime();
                rlImGui.Begin(dt);

                if (_modPanel.IsOpen)
                    _modPanel.Render();

                if (state == GameState.GameOver)
                {
                    _gameOverScreen.Render(_waveManager.CurrentWave, _gameStats.TotalKills);

                    if (_gameOverScreen.RestartRequested)
                        ResetGame();
                }

                rlImGui.End();
            }

            Raylib.DrawFPS(Raylib.GetScreenWidth() - 100, 10);
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

        private static Color GetProjectileColor(ModTags tags)
        {
            if (tags.HasFlag(ModTags.Burn)) return new Color(255, 120, 30, 255);
            if (tags.HasFlag(ModTags.Frost)) return new Color(51, 153, 255, 255);
            if (tags.HasFlag(ModTags.Shock)) return new Color(255, 242, 51, 255);
            if (tags.HasFlag(ModTags.Void)) return new Color(160, 50, 255, 255);
            if (tags.HasFlag(ModTags.Heavy)) return new Color(255, 80, 80, 255);
            if (tags.HasFlag(ModTags.Swift)) return new Color(100, 255, 180, 255);
            return new Color(0, 255, 255, 255);
        }

        private static Color GetRarityColor(Rarity rarity) => rarity switch
        {
            Rarity.Common => new Color(200, 200, 200, 255),
            Rarity.Uncommon => new Color(100, 255, 100, 255),
            Rarity.Rare => new Color(100, 150, 255, 255),
            Rarity.Epic => new Color(200, 100, 255, 255),
            _ => new Color(200, 200, 200, 255)
        };

        private static void DrawOctahedronWires(Vector3 center, float radiusH, float radiusV, Color color)
        {
            DrawOctahedronWiresRotated(center, radiusH, radiusV, 0f, color);
        }

        private static void DrawOctahedronWiresRotated(Vector3 center, float radiusH, float radiusV, float angleY, Color color)
        {
            float cos = MathF.Cos(angleY);
            float sin = MathF.Sin(angleY);

            var top = center + new Vector3(0, radiusV, 0);
            var bottom = center - new Vector3(0, radiusV, 0);
            var right = center + new Vector3(radiusH * cos, 0, radiusH * sin);
            var left = center - new Vector3(radiusH * cos, 0, radiusH * sin);
            var front = center + new Vector3(-radiusH * sin, 0, radiusH * cos);
            var back = center - new Vector3(-radiusH * sin, 0, radiusH * cos);

            Raylib.DrawLine3D(top, right, color);
            Raylib.DrawLine3D(top, left, color);
            Raylib.DrawLine3D(top, front, color);
            Raylib.DrawLine3D(top, back, color);
            Raylib.DrawLine3D(bottom, right, color);
            Raylib.DrawLine3D(bottom, left, color);
            Raylib.DrawLine3D(bottom, front, color);
            Raylib.DrawLine3D(bottom, back, color);
            Raylib.DrawLine3D(right, front, color);
            Raylib.DrawLine3D(front, left, color);
            Raylib.DrawLine3D(left, back, color);
            Raylib.DrawLine3D(back, right, color);
        }

        private static Color UintToColor(uint rgba)
        {
            byte r = (byte)((rgba >> 24) & 0xFF);
            byte g = (byte)((rgba >> 16) & 0xFF);
            byte b = (byte)((rgba >> 8) & 0xFF);
            byte a = (byte)(rgba & 0xFF);
            return new Color(r, g, b, a);
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
