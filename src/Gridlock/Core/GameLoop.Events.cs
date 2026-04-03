using System;
using System.Numerics;
using Gridlock.Audio;
using Gridlock.Enemies;
using Gridlock.Mods.Pipeline;
using Gridlock.Towers;
using Gridlock.Visual;
using Raylib_cs;
using Color = Raylib_cs.Color;

namespace Gridlock.Core
{
    public sealed partial class GameLoop
    {
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

        private void OnWaveCleared(int waveNumber)
        {
            _gameStats.SetWave(waveNumber);

            _particles.BurstSphere(_gridManager.ObjectivePosition + new Vector3(0, 2, 0),
                40, 6f, 3f, 1.5f, new Color(0, 255, 200, 255));

            if (_warpManager.Initialized)
                _warpManager.Shockwave(_gridManager.ObjectivePosition, 5f, 10f, new Color(0, 255, 200, 255));

            _soundManager.Play(SoundType.WaveComplete);
            _soundManager.Play(SoundType.UIAnnounce);

            _hud.ShowAnnouncement($"WAVE_{waveNumber:D2}_CLEARED");

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
            _projectileTrails.Clear();
            _trails.Clear();
            _particles.Clear();
            _voxelPool.Clear();
            _impactFlash.Clear();
            _damageText.Clear();
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
    }
}
