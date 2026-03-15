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

        [Header("Indicator GO")]
        [Tooltip("Výchozí velikost GO indikátoru v canvas pixelech")]
        public Vector2 defaultSize = new Vector2(50f, 50f);

        [Tooltip("Scale GO při suspicion = 100")]
        [Range(0f, 5f)]
        public float scaleMax = 3f;

        [Header("Colors")]
        public Color colorLow = new Color(1f, 0.85f, 0f);
        public Color colorMedium = new Color(1f, 0.50f, 0f);
        public Color colorHigh = new Color(1f, 0.10f, 0.1f);

        [Header("Threshold")]
        [Tooltip("Minimální změna suspicion pro překreslení (optimalizace)")]
        [Range(0.001f, 5f)]
        public float changeThreshold = 0.5f;
    }
}