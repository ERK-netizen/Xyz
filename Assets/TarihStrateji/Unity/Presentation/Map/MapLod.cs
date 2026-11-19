using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Harita ayrıntı düzeyi (LOD 0–3). Düzey L'de bir parça 2^L × 2^L hücrelik bloklarla çizilir:
    /// LOD 0 tam altıgen, 1–3 blok dörtgenleri. Seçim kameranın dikey yarı boyuna (dünya birimi)
    /// göredir; titremeyi önlemek için %15 gecikme (histerezis) uygulanır.
    /// </summary>
    public static class MapLod
    {
        public const int MaxLevel = 3;
        private const float Hysteresis = 0.15f;

        // Düzey L'den L+1'e geçiş eşiği (orthographicSize). Dikey ekranda ~24 satır görünürken LOD 0.
        private static readonly float[] Thresholds = { 26f, 60f, 140f };

        public static int Step(int level) => 1 << Mathf.Clamp(level, 0, MaxLevel);

        public static int LevelFor(float orthographicSize, int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, 0, MaxLevel);
            while (level < MaxLevel && orthographicSize > Thresholds[level] * (1f + Hysteresis))
            {
                level++;
            }

            while (level > 0 && orthographicSize < Thresholds[level - 1] * (1f - Hysteresis))
            {
                level--;
            }

            return level;
        }
    }
}
