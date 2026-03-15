using System.Collections.Generic;
using UnityEngine;

namespace ZeroTrace.Audio
{
    /// <summary>
    /// Object pool for AudioSources.
    /// Single Responsibility: lifecycle of AudioSource GameObjects only.
    /// No singleton — owned and accessed exclusively via AudioManager.
    /// </summary>
    public sealed class AudioSourcePool
    {
        private readonly Queue<AudioSource> _pool;
        private readonly Transform _container;
        private readonly int _growBy;

        public int Available => _pool.Count;

        public AudioSourcePool(Transform container, int initialSize = 20, int growBy = 5)
        {
            _container = container;
            _growBy = growBy;
            _pool = new Queue<AudioSource>(initialSize);
            Grow(initialSize);
        }

        // ── Public API ──────────────────────────────────────────────────────

        public AudioSource Rent()
        {
            if (_pool.Count == 0)
                Grow(_growBy);

            AudioSource source = _pool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        public void Return(AudioSource source)
        {
            if (source == null) return;  // přidej toto

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.transform.SetParent(_container, false);
            source.gameObject.SetActive(false);
            _pool.Enqueue(source);
        }

        // ── Private ─────────────────────────────────────────────────────────

        private void Grow(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("AS_Pooled");
                go.transform.SetParent(_container, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                go.SetActive(false);
                _pool.Enqueue(source);
            }
        }
    }
}