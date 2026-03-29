using System.Collections.Generic;
using Gridlock.Core;
using UnityEngine;

namespace Gridlock.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] private SoundLibrary library;
        [SerializeField] private int poolSize = 20;

        [Header("Global")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

        private readonly List<AudioSource> _pool = new();
        private readonly Dictionary<SoundType, InstanceTracker> _trackers = new();
        private AudioSource _uiSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ServiceLocator.Register(this);

            for (int i = 0; i < poolSize; i++)
                _pool.Add(CreateSource());

            var uiGo = new GameObject("UIAudioSource");
            uiGo.transform.SetParent(transform);
            _uiSource = uiGo.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.spatialBlend = 0f;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<SoundManager>();
                Instance = null;
            }

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnGameStateChanged;
        }

        public void PlayUI(SoundType type)
        {
            if (library == null || _uiSource == null) return;

            var entry = library.Get(type);
            if (entry == null) return;

            var clip = entry.GetRandomClip();
            if (clip == null) return;

            _uiSource.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
            _uiSource.PlayOneShot(clip, entry.volume * uiVolume * masterVolume);
        }

        public void Play(SoundType type)
        {
            PlayInternal(type, null);
        }

        public void Play(SoundType type, Vector3 position)
        {
            PlayInternal(type, position);
        }

        private void PlayInternal(SoundType type, Vector3? position)
        {
            if (library == null) return;

            var entry = library.Get(type);
            if (entry == null) return;

            var clip = entry.GetRandomClip();
            if (clip == null) return;

            var tracker = GetTracker(type);

            if (tracker.IsOnCooldown(entry.cooldown))
                return;

            if (entry.maxInstances > 0 && tracker.activeCount >= entry.maxInstances)
            {
                var stolen = tracker.StealOldest();
                if (stolen != null)
                    stolen.Stop();
            }

            var source = GetAvailableSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = entry.volume * sfxVolume * masterVolume;
            source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
            source.priority = entry.priority;
            source.spatialBlend = entry.spatialBlend;

            if (position.HasValue && entry.spatialBlend > 0f)
                source.transform.position = position.Value;

            source.Play();
            tracker.Register(source);
        }

        private AudioSource GetAvailableSource()
        {
            foreach (var source in _pool)
            {
                if (!source.isPlaying)
                    return source;
            }

            if (_pool.Count < poolSize * 2)
            {
                var source = CreateSource();
                _pool.Add(source);
                return source;
            }

            return null;
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("SoundEmitter");
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = 50f;
            return source;
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

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (current == GameState.Wave && prev == GameState.Preparing)
                Play(SoundType.WaveStart);
            else if (current == GameState.Preparing && prev == GameState.Wave)
                Play(SoundType.WaveComplete);
        }

        private class InstanceTracker
        {
            public int activeCount;
            private float _lastPlayTime = -1000f;
            private readonly List<AudioSource> _activeSources = new();

            public bool IsOnCooldown(float cooldown)
            {
                return Time.unscaledTime - _lastPlayTime < cooldown;
            }

            public void Register(AudioSource source)
            {
                _activeSources.Add(source);
                activeCount++;
                _lastPlayTime = Time.unscaledTime;
            }

            public AudioSource StealOldest()
            {
                Cleanup();
                if (_activeSources.Count == 0) return null;
                var oldest = _activeSources[0];
                _activeSources.RemoveAt(0);
                activeCount = _activeSources.Count;
                return oldest;
            }

            public void Cleanup()
            {
                _activeSources.RemoveAll(s => s == null || !s.isPlaying);
                activeCount = _activeSources.Count;
            }
        }

        private void Update()
        {
            foreach (var tracker in _trackers.Values)
                tracker.Cleanup();
        }
    }
}
