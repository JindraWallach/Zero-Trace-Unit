using UnityEngine;

namespace ZeroTrace.UI.Suspicion
{
    /// <summary>
    /// Single SO pro všechna nastavení suspicion HUD ikonek.
    /// Create via: Assets > Create > Zero Trace > UI > Suspicion Indicator Config
    /// </summary>
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
        public Vector2 indicatorSize = new Vector2(80f, 12f);

        [Header("Bar Colors")]
        public Color colorLow = new Color(1f, 0.85f, 0f);
        public Color colorMedium = new Color(1f, 0.50f, 0f);
        public Color colorHigh = new Color(1f, 0.10f, 0.1f);

        [Header("Bar Threshold")]
        [Tooltip("Minimální změna fill (0-1) pro překreslení baru")]
        [Range(0.001f, 0.05f)]
        public float fillChangeThreshold = 0.005f;
    }
}