using UnityEngine;

namespace ZeroTrace.UI.Suspicion
{
    [CreateAssetMenu(
        fileName = "SuspicionIndicatorConfig",
        menuName = "Zero Trace/UI/Suspicion Indicator Config")]
    public sealed class SuspicionIndicatorConfig : ScriptableObject
    {
        [Header("Positioner")]
        [Tooltip("Jak vysoko nad pivotem enemy ikona plave (world units)")]
        [Range(0f, 5f)]
        public float worldHeightOffset = 2.4f;

        [Header("Bar Layout")]
        [Tooltip("Velikost indikatoru v canvas pixelech")]
        public Vector2 indicatorSize = new Vector2(50f, 50f);

        [Header("Bar Scale")]
        [Tooltip("Scale ikony při suspicion = 0")]
        [Range(0f, 3f)]
        public float scaleMin = 0f;

        [Tooltip("Scale ikony při suspicion = 100")]
        [Range(0f, 5f)]
        public float scaleMax = 3f;

        [Tooltip("Výchozí scale při spawnu (než enemy uvidí hráče)")]
        [Range(0f, 3f)]
        public float scaleDefault = 1f;

        [Header("Bar Colors")]
        public Color colorLow = new Color(1f, 0.85f, 0f);
        public Color colorMedium = new Color(1f, 0.50f, 0f);
        public Color colorHigh = new Color(1f, 0.10f, 0.1f);

        [Header("Threshold")]
        [Tooltip("Minimální změna suspicion pro překreslení (optimalizace)")]
        [Range(0.001f, 5f)]
        public float changeThreshold = 0.5f;
    }
}