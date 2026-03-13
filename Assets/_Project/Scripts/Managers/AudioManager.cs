using System.Collections.Generic;
using UnityEngine;

namespace ZeroTrace.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Pool Settings")]
        [SerializeField] private int poolInitialSize = 20;
        [SerializeField] private int poolGrowBy = 5;

        [Header("Category Volumes")]
        [SerializeField, Range(0f, 1f)] private float volumeSFX = 1f;
        [SerializeField, Range(0f, 1f)] private float volumeBGM = 0.8f;
        [SerializeField, Range(0f, 1f)] private float volumeAmbient = 0.7f;
        [SerializeField, Range(0f, 1f)] private float volumeVoice = 1f;
        [SerializeField, Range(0f, 1f)] private float volumeUI = 1f;

        // ── Private state ────────────────────────────────────────────────────

        private AudioSourcePool _pool;
        private Dictionary<string, AudioData> _dataMap;
        private readonly List<PlaySoundCommand> _active = new(32);

        private const string ResourcesPath = "Audio";

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            var container = new GameObject("_AudioPool").transform;
            container.SetParent(transform, false);

            _pool = new AudioSourcePool(container, poolInitialSize, poolGrowBy);

            AudioData[] loaded = Resources.LoadAll<AudioData>(ResourcesPath);
            _dataMap = new Dictionary<string, AudioData>(loaded.Length);

            foreach (var data in loaded)
            {
                if (data == null || string.IsNullOrEmpty(data.id)) continue;
                if (!_dataMap.TryAdd(data.id, data))
                    Debug.LogWarning($"[AudioManager] Duplicate audio id: '{data.id}'");
            }

            Debug.Log($"[AudioManager] Loaded {_dataMap.Count} AudioData from Resources/{ResourcesPath}/");
        }

        private void OnDestroy() => StopAll();

        // ── Public API ───────────────────────────────────────────────────────

        public PlaySoundCommand Play(string id, Vector3 position = default)
        {
            if (!_dataMap.TryGetValue(id, out AudioData data))
            {
                Debug.LogWarning($"[AudioManager] Unknown id: '{id}'");
                return null;
            }

            float finalVolume = data.volume * GetCategoryVolume(data.category);
            AudioSource source = _pool.Rent();
            source.transform.position = position;

            var cmd = new PlaySoundCommand(data, source, _pool, this, RemoveCommand, finalVolume);
            cmd.Execute();
            _active.Add(cmd);
            return cmd;
        }

        public void Stop(PlaySoundCommand cmd) => cmd?.Release();

        public void StopAll(string excludeId = null)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var cmd = _active[i];
                if (excludeId != null && cmd.Data.id == excludeId) continue;
                cmd.Release();
            }
        }

        public void StopCategory(AudioCategory category)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Data.category == category)
                    _active[i].Release();
            }
        }

        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (category)
            {
                case AudioCategory.SFX: volumeSFX = volume; break;
                case AudioCategory.BGM: volumeBGM = volume; break;
                case AudioCategory.Ambient: volumeAmbient = volume; break;
                case AudioCategory.Voice: volumeVoice = volume; break;
                case AudioCategory.UI: volumeUI = volume; break;
            }
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void RemoveCommand(PlaySoundCommand cmd) => _active.Remove(cmd);

        private float GetCategoryVolume(AudioCategory cat) => cat switch
        {
            AudioCategory.SFX => volumeSFX,
            AudioCategory.BGM => volumeBGM,
            AudioCategory.Ambient => volumeAmbient,
            AudioCategory.Voice => volumeVoice,
            AudioCategory.UI => volumeUI,
            _ => 1f
        };
    }
}