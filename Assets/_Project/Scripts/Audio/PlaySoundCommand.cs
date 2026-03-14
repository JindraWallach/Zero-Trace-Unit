using System;
using System.Collections;
using UnityEngine;

namespace ZeroTrace.Audio
{
    /// <summary>
    /// Command pattern: one play-and-return-to-pool operation.
    /// finalVolume = data.volume * categoryVolume — SO zůstává immutable.
    /// </summary>
    public sealed class PlaySoundCommand
    {
        public AudioData Data { get; }

        private readonly AudioSource _source;
        private readonly AudioSourcePool _pool;
        private readonly MonoBehaviour _runner;
        private readonly Action<PlaySoundCommand> _onComplete;
        private readonly float _finalVolume;

        private Coroutine _coroutine;

        /// <param name="finalVolume">Výsledná hlasitost (data.volume * kategorie). -1 = použij data.volume.</param>
        public PlaySoundCommand(
            AudioData data,
            AudioSource source,
            AudioSourcePool pool,
            MonoBehaviour runner,
            Action<PlaySoundCommand> onComplete,
            float finalVolume = -1f)
        {
            Data = data;
            _source = source;
            _pool = pool;
            _runner = runner;
            _onComplete = onComplete;
            _finalVolume = finalVolume < 0f ? data.volume : finalVolume;
        }

        public void Execute()
        {
            ApplyData();
            _source.Play();
            Debug.Log($"[PlaySoundCommand] Playing '{Data.id}' with volume {_finalVolume} and pitch {_source.pitch}");

            if (!Data.loop)
                _coroutine = _runner.StartCoroutine(WaitAndRelease());
        }

        public void Release()
        {
            if (_coroutine != null)
            {
                _runner.StopCoroutine(_coroutine);
                _coroutine = null;
            }
            _pool.Return(_source);
            _onComplete?.Invoke(this);
        }

        // ── Private ─────────────────────────────────────────────────────────

        private void ApplyData()
        {
            _source.clip = Data.clip;
            _source.volume = _finalVolume;
            _source.pitch = (Data.pitchMin >= Data.pitchMax)
                                       ? Data.pitchMin
                                       : UnityEngine.Random.Range(Data.pitchMin, Data.pitchMax);
            _source.loop = Data.loop;
            _source.spatialBlend = Data.spatialBlend;
            _source.minDistance = Data.minDistance;
            _source.maxDistance = Data.maxDistance;
        }

        private IEnumerator WaitAndRelease()
        {
            float duration = Data.clip.length / Mathf.Abs(_source.pitch);
            yield return new WaitForSeconds(duration);
            Release();
        }

        public void Pause()
        {
            _source.Pause();
        }

        public void Resume()
        {
            _source.UnPause();
        }
    }
}