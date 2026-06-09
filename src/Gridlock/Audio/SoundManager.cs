using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Core;
using Raylib_cs;

namespace Gridlock.Audio
{
    public sealed class SoundManager
    {
        private readonly Dictionary<SoundType, SoundConfig> _configs = new();
        private readonly Dictionary<SoundType, Sound[]> _sounds = new();
        private readonly Dictionary<SoundType, InstanceTracker> _trackers = new();
        private readonly Random _rng = new();

        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _uiVolume = 1f;

        private float _cameraWorldX;
        private float _cameraViewWidth = 48f;

        private static readonly HashSet<SoundType> UISoundTypes = new()
        {
            SoundType.NodeGrab, SoundType.NodeDrop, SoundType.NodeRemove,
            SoundType.PortConnect, SoundType.PortDisconnect,
            SoundType.EditorOpen, SoundType.EditorClose,
            SoundType.InventoryOpen, SoundType.InventoryClose,
            SoundType.UIDragStart, SoundType.UIDragHover, SoundType.UIDropFail,
            SoundType.UIClick, SoundType.UIHover, SoundType.UIAnnounce,
            SoundType.UIDropdownOpen, SoundType.UIDropdownSelect
        };

        public void Init(Dictionary<SoundType, SoundConfig> configs)
        {
            foreach (var (type, config) in configs)
            {
                _configs[type] = config;
                var loaded = new List<Sound>();
                foreach (var path in config.FilePaths)
                {
                    if (System.IO.File.Exists(path))
                        loaded.Add(Raylib.LoadSound(path));
                }
                if (loaded.Count > 0)
                    _sounds[type] = loaded.ToArray();
            }

            ServiceLocator.Register(this);
        }

        public void LoadFromJson(string jsonPath)
        {
            if (!System.IO.File.Exists(jsonPath)) return;
            try
            {
                var json = System.IO.File.ReadAllText(jsonPath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("masterVolume", out var mv)) _masterVolume = mv.GetSingle();
                if (root.TryGetProperty("sfxVolume", out var sv)) _sfxVolume = sv.GetSingle();
                if (root.TryGetProperty("uiVolume", out var uv)) _uiVolume = uv.GetSingle();

                if (root.TryGetProperty("sounds", out var sounds))
                {
                    foreach (var prop in sounds.EnumerateObject())
                    {
                        if (!Enum.TryParse<SoundType>(prop.Name, out var soundType)) continue;
                        var entry = prop.Value;

                        var files = new List<string>();
                        if (entry.TryGetProperty("files", out var filesArr))
                            foreach (var f in filesArr.EnumerateArray())
                                files.Add("resources/" + f.GetString()!);

                        var config = new SoundConfig
                        {
                            FilePaths = files.ToArray(),
                            Volume = entry.TryGetProperty("volume", out var v) ? v.GetSingle() : 1f,
                            PitchVariance = entry.TryGetProperty("pitchVariance", out var pv) ? pv.GetSingle() : 0.1f,
                            Cooldown = entry.TryGetProperty("cooldown", out var cd) ? cd.GetSingle() : 0.05f,
                            MaxInstances = entry.TryGetProperty("maxInstances", out var mi) ? mi.GetInt32() : 3,
                        };

                        _configs[soundType] = config;
                        var loaded = new List<Sound>();
                        foreach (var path in config.FilePaths)
                        {
                            if (System.IO.File.Exists(path))
                                loaded.Add(Raylib.LoadSound(path));
                        }
                        if (loaded.Count > 0)
                        {
                            // Unload any sounds a prior Init() loaded for this type before replacing
                            // the array, otherwise those audio buffers leak for the process lifetime.
                            if (_sounds.TryGetValue(soundType, out var previous))
                                foreach (var s in previous)
                                    Raylib.UnloadSound(s);
                            _sounds[soundType] = loaded.ToArray();
                        }
                    }
                }
                Console.WriteLine($"[SoundManager] Loaded {_sounds.Count} sound types from {jsonPath}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[SoundManager] Failed to load {jsonPath}: {e.Message}");
            }
        }

        public void Play(SoundType type, float volumeScale = 1f, Vector3? worldPos = null)
        {
            if (!_sounds.TryGetValue(type, out var variants)) return;
            if (!_configs.TryGetValue(type, out var config)) return;

            var tracker = GetTracker(type);
            if (tracker.IsOnCooldown(config.Cooldown)) return;

            if (config.MaxInstances > 0 && tracker.ActiveCount >= config.MaxInstances)
                return;

            var sound = variants[_rng.Next(variants.Length)];

            bool isUI = UISoundTypes.Contains(type);
            float categoryVolume = isUI ? _uiVolume : _sfxVolume;
            float finalVolume = config.Volume * categoryVolume * _masterVolume * volumeScale;
            Raylib.SetSoundVolume(sound, Math.Clamp(finalVolume, 0f, 1f));

            float pitch = 1f + ((float)_rng.NextDouble() * 2f - 1f) * config.PitchVariance;
            Raylib.SetSoundPitch(sound, Math.Clamp(pitch, 0.1f, 3f));

            if (worldPos.HasValue)
            {
                float pan = WorldToPan(worldPos.Value.X);
                Raylib.SetSoundPan(sound, pan);
            }
            else
            {
                Raylib.SetSoundPan(sound, 0.5f);
            }

            Raylib.PlaySound(sound);
            float lengthSeconds = sound.FrameCount > 0 && sound.Stream.SampleRate > 0
                ? (float)sound.FrameCount / sound.Stream.SampleRate
                : 0.25f;
            tracker.Register(Raylib.GetTime() + lengthSeconds);
        }

        public void Update()
        {
            foreach (var tracker in _trackers.Values)
                tracker.Tick();
        }

        public void SetMasterVolume(float volume) => _masterVolume = Math.Clamp(volume, 0f, 1f);
        public void SetSFXVolume(float volume) => _sfxVolume = Math.Clamp(volume, 0f, 1f);
        public void SetUIVolume(float volume) => _uiVolume = Math.Clamp(volume, 0f, 1f);
        public void SetCameraInfo(float worldX, float viewWidth)
        {
            _cameraWorldX = worldX;
            _cameraViewWidth = Math.Max(viewWidth, 1f);
        }

        private float WorldToPan(float worldX)
        {
            float offset = worldX - _cameraWorldX;
            float halfWidth = _cameraViewWidth * 0.5f;
            float normalized = offset / halfWidth;
            return Math.Clamp(normalized * 0.25f + 0.5f, 0f, 1f);
        }

        private InstanceTracker GetTracker(SoundType type)
        {
            if (!_trackers.TryGetValue(type, out var tracker))
            {
                tracker = new InstanceTracker();
                _trackers[type] = tracker;
            }
            return tracker;
        }

        public void Shutdown()
        {
            foreach (var variants in _sounds.Values)
            {
                foreach (var sound in variants)
                    Raylib.UnloadSound(sound);
            }
            _sounds.Clear();
            _configs.Clear();
            _trackers.Clear();
            ServiceLocator.Unregister<SoundManager>();
        }

        private sealed class InstanceTracker
        {
            private readonly List<double> _endTimes = new();
            private double _lastPlayTime = -1000.0;

            // Instances still audibly playing (their end time is in the future). Decays as sounds
            // finish rather than being blanket-reset each frame, so MaxInstances limits real polyphony.
            public int ActiveCount => _endTimes.Count;

            public bool IsOnCooldown(float cooldown)
            {
                return Raylib.GetTime() - _lastPlayTime < cooldown;
            }

            public void Register(double endTime)
            {
                _endTimes.Add(endTime);
                _lastPlayTime = Raylib.GetTime();
            }

            public void Tick()
            {
                double now = Raylib.GetTime();
                for (int i = _endTimes.Count - 1; i >= 0; i--)
                {
                    if (_endTimes[i] <= now)
                    {
                        int last = _endTimes.Count - 1;
                        _endTimes[i] = _endTimes[last];
                        _endTimes.RemoveAt(last);
                    }
                }
            }
        }
    }
}
