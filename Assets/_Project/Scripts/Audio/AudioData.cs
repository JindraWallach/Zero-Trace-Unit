using UnityEngine;

namespace ZeroTrace.Audio
{
    /// <summary>
    /// Immutable audio configuration. One SO per sound type (BGM, FX, Voice, Ambient...).
    /// Single Responsibility: data-only, zero logic.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioData", menuName = "Zero Trace/Audio/AudioData")]
    public class AudioData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public AudioCategory category = AudioCategory.SFX;

        [Header("Clip")]
        public AudioClip clip;

        [Header("Volume & Pitch")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitchMin = 1f;
        [Range(0.1f, 3f)] public float pitchMax = 1f;

        [Header("Behaviour")]
        public bool loop = false;

        [Header("Spatialization")]
        [Range(0f, 1f)] public float spatialBlend = 0f;   // 0 = 2D, 1 = 3D
        public float minDistance = 1f;
        public float maxDistance = 500f;

        private void OnValidate()
        {
            if (pitchMax < pitchMin) pitchMax = pitchMin;
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);
        }
    }

    public enum AudioCategory
    {
        SFX,
        BGM,
        Ambient,
        Voice,
        UI
    }
}